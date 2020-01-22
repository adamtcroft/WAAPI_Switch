using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace WAAPI_Switch
{
    class MainClassDay3
    {
        public static void MainDay3(string[] args)
        {
            _MainDay3().Wait();
        }

        static async Task _MainDay3()
        {
            // Again, using .Result to return values
            var client = CreateConnection().Result;
            var switches = GetSwitchObjects(client).Result;

            // These have no return value
            await AssignSwitchContainers(client, switches);
            await GetSwitchAssignments(client, switches);
            await client.Close();
        }

        // This new funciton queries for all the assignments of your Switch Containers.
        // This data is now added to your SwitchContainer objects
        private static async Task GetSwitchAssignments(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            // For every parent switch container in our SwitchCollection...
            foreach (var container in switches.containers)
            {
                // Use WAAPI to get all of its assignments
                var result = await client.Call(
                    ak.wwise.core.switchContainer.getAssignments,
                    new JObject
                    (    
                        new JProperty("id", container.id)
                    ),
                    null
                );  

                // If any assignments exist, add them to the container.assignments list
                if (result["return"].Count() > 0)
                {
                    var tokens = result["return"];
                    foreach (var assignment in tokens)
                    {
                        Console.WriteLine("Adding assignment " + assignment["child"]);

                        // This add call references the new SwitchAssignment object -
                        // you can find this defined in SwitchContainer.cs
                        container.assignments.Add(assignment.ToObject<SwitchAssignment>());
                    }
                }
            }
        }

        // This function is the same as before, still assigning parent containers to groups
        private static async Task AssignSwitchContainers(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            foreach (var container in switches.containers)
            {
                var result = await client.Call(
                    ak.wwise.core.@object.get,
                    new JObject
                    (
                       new JProperty("from", new JObject(new JProperty("id", new JArray(new string[] { container.id }))))
                    ),
                    new JObject
                    (
                        new JProperty("return", new string[] { "@SwitchGroupOrStateGroup" })
                    ));

                var token = result["return"][0]["@SwitchGroupOrStateGroup"];

                if (token["id"].ToString() == "{00000000-0000-0000-0000-000000000000}"
                    || token["id"].ToString() != switches.groups.Find(group => group.name == container.name).name)
                {
                    foreach (var group in switches.groups)
                    {
                        if (container.name == group.name)
                        {
                            await client.Call(
                                ak.wwise.core.@object.setReference,
                                new JObject
                                (
                                    new JProperty("reference", "SwitchGroupOrStateGroup"),
                                    new JProperty("object", container.id),
                                    new JProperty("value", group.id)
                                ),
                                null
                            );
                        }
                    }
                }
            }
        }

        // This function is now different - to move forward, you need information on the children of your
        // groups and containers
        private static async Task<SwitchCollection> GetSwitchObjects(AK.Wwise.Waapi.JsonClient client)
        {
            SwitchCollection switchCollection = new SwitchCollection();

            try
            {
                // This query is the same
                var results = await client.Call(
                    ak.wwise.core.@object.get,
                    new JObject
                    (
                    new JProperty("from", new JObject
                        (
                            new JProperty("ofType", new JArray(new string[] { "SwitchContainer", "SwitchGroup" }))
                        ))
                    ),
                    new JObject
                    (
                        new JProperty("return", new string[] { "name", "id", "type" })
                    )
                );

                var tokens = results["return"];

                List<SwitchContainer> containers = new List<SwitchContainer>();
                List<SwitchGroup> groups = new List<SwitchGroup>();

                foreach (var token in tokens)
                {
                    // If we've got a Switch Container...
                    if (token["type"].ToString() == "SwitchContainer")
                    {
                        // Turn the container into an object (rather than doing that and inserting it into the list
                        // at the same time)
                        var container = token.ToObject<SwitchContainer>();

                        // Query for all the child objects of the container - asking for their name and GUID
                        var containerResults = await client.Call(
                            ak.wwise.core.@object.get,
                            new JObject
                            (
                                new JProperty("from", new JObject
                                (
                                    new JProperty("id", new JArray(new string[] { container.id }))
                                )),
                                new JProperty("transform", new JArray(new JObject
                                (
                                    new JProperty("select", new JArray(new string[] { "children" }))
                                )))
                            ),
                            new JObject
                            (
                                new JProperty("return", new string[] { "name", "id" })
                            )
                        );

                        // Add each child to the SwitchContainer object
                        foreach (var switchToken in containerResults["return"])
                        {
                            Console.WriteLine("Adding child " + switchToken["name"] + " to " + container.name);
                            container.children.Add(switchToken.ToObject<SwitchContainerChild>());
                        }
                        // Add the SwitchContainer to the SwitchCollection's list of containers
                        containers.Add(container);
                    }

                    // If we've got a group...
                    if (token["type"].ToString() == "SwitchGroup")
                    {
                        // Turn the group into an object (rather than doing that and inserting it into the list
                        // at the same time)
                        var group = token.ToObject<SwitchGroup>();

                        // Query for all the child objects of the group - asking for their name and GUID
                        var groupResults = await client.Call(
                            ak.wwise.core.@object.get,
                            new JObject
                            (
                                new JProperty("from", new JObject
                                (
                                    new JProperty("id", new JArray(new string[] { group.id }))
                                )),
                                new JProperty("transform", new JArray(new JObject
                                (
                                    new JProperty("select", new JArray(new string[] { "children" }))
                                )))
                            ),
                            new JObject
                            (
                                new JProperty("return", new string[] { "name", "id" })
                            )
                        );

                        // Add each child to the SwitchGroup object
                        foreach (var switchToken in groupResults["return"])
                        {
                            Console.WriteLine("Adding switch " + switchToken["name"] + " to " + group.name);
                            group.switches.Add(switchToken.ToObject<WwiseSwitch>());
                        }
                        // Add the SwitchGroup to the SwitchCollection's list of containers
                        groups.Add(group);
                    }
                }

                groups = groups.Where(group => containers.Any(container => container.name == group.name)).ToList();

                switchCollection.containers = containers;
                switchCollection.groups = groups;
            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }

            return switchCollection;
        }

        private static async Task<AK.Wwise.Waapi.JsonClient> CreateConnection()
        {
            AK.Wwise.Waapi.JsonClient client = new AK.Wwise.Waapi.JsonClient();

            await client.Connect();

            client.Disconnected += () =>
            {
                System.Console.WriteLine("We lost connection!");
            };

            return client;
        }

    }
}
