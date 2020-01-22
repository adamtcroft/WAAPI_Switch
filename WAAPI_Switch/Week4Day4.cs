using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace WAAPI_Switch
{
    class MainClassDay4
    {
        public static void MainDay4(string[] args)
        {
            _MainDay4().Wait();
        }

        static async Task _MainDay4()
        {
            var client = CreateConnection().Result;
            var switches = GetSwitchObjects(client).Result;
            await AssignSwitchContainers(client, switches);
            await GetSwitchAssignments(client, switches);
            await SetSwitchAssignments(client, switches);
            await client.Close();
        }

        // This function matches and assigns all unassigned switches
        private static async Task MatchAndAssignSwitches(AK.Wwise.Waapi.JsonClient client, List<SwitchContainerChild> children,
            SwitchGroup matchingGroup)
        {
            // For each child object of the switch container...
            foreach (var item in children)
            {
                // Grab the switch where the switch name equals the child name
                var matchingSwitch = matchingGroup.switches.Where(s => s.name == item.name).First();

                // Use WAAPI to add the assignment
                await client.Call(
                    ak.wwise.core.switchContainer.addAssignment,
                    new JObject
                    (
                        new JProperty("stateOrSwitch", matchingSwitch.id),
                        new JProperty("child", item.id)
                    )
                );
            }
        }

        // This function checks existing assignments and removes any incorrecet ones
        private static async Task CheckAssignments(AK.Wwise.Waapi.JsonClient client, List<SwitchContainerChild> assigned,
            List<SwitchContainerChild> unassigned, SwitchGroup matchingGroup, SwitchContainer container)
        {
            // Create a new list of SwitchAssignments for those that are incorrect
            // This is necessary because C# won't let you remove from a list while you're
            // looping through it.
            List<SwitchAssignment> toRemove = new List<SwitchAssignment>();

            // For each child container...
            foreach (var child in assigned)
            {
                // Grab the matching switch object
                var matchingSwitch = matchingGroup.switches.Where(s => s.name == child.name).First();
                // Grab the matching assignment object
                var assignment = container.assignments.Where(a => a.child == child.id).First();

                // if the matching switch and assignment id's aren't the same...
                if (matchingSwitch.id != assignment.stateOrSwitch)
                {
                    // Add the assignment to the list to remove
                    toRemove.Add(assignment);

                    // Note the child object as now unassigned
                    unassigned.Add(child);

                    // Actually remove the assignment in Wwise with WAAPI
                    await client.Call(
                        ak.wwise.core.switchContainer.removeAssignment,
                        new JObject
                        (
                            new JProperty("stateOrSwitch", assignment.stateOrSwitch),
                            new JProperty("child", assignment.child)
                        ),
                        null
                    );
                }
            }
            // Rebuild the list of assignments by getting rid of the assignments that were removed
            // via the loop
            container.assignments = container.assignments.Except(toRemove).ToList();
        }

        // This function figures out which switches have been assigned, and which have been assigned incorrectly.
        // From there, the correct assignments (where the name of the switch and name of the child container match)
        // are made.
        // Depending on how your assignments currently exist - this function potentially has plenty of bugs.
        // It hasn't been tested for multiple assignment (as you can assign multiple child containers to a switch).
        // But, within the realm of our requirements (unassigning single incorrect containers or assigning containers
        // and switches where there are no assignments) - it works.
        private static async Task SetSwitchAssignments(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            // For each SwitchContainer
            foreach (var container in switches.containers)
            {
                // Use LINQ to get a list of children where no switch assignment has been made
                var unassigned = container.children.Where(c => !container.assignments.Exists(a => a.child == c.id)).ToList();

                // Get a list of all the assigned groups by comparing all groups to the unassigned list
                var assigned = container.children.Except(unassigned).ToList();

                // Use LINQ to grab the SwitchGroup whose name matches the current container we're looping through
                var matchingGroup = switches.groups.Where(g => g.name == container.name).First();

                // If any object have been assigned...
                if (assigned.Count > 0)
                {
                    // Check to see that the names match correctly
                    await CheckAssignments(client, assigned, unassigned, matchingGroup, container);
                }

                // If there are any unassigned...
                if (unassigned.Count > 0)
                {
                    // Assign them
                    await MatchAndAssignSwitches(client, unassigned, matchingGroup);
                }
            }
        }

        private static async Task GetSwitchAssignments(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            foreach (var container in switches.containers)
            {
                var result = await client.Call(
                    ak.wwise.core.switchContainer.getAssignments,
                    new JObject
                    (
                        new JProperty("id", container.id)
                    ),
                    null
                );

                if (result["return"].Count() > 0)
                {
                    var tokens = result["return"];
                    foreach (var assignment in tokens)
                    {
                        Console.WriteLine("Adding assignment " + assignment["child"]);
                        container.assignments.Add(assignment.ToObject<SwitchAssignment>());
                    }
                }
            }
        }

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

        private static async Task<SwitchCollection> GetSwitchObjects(AK.Wwise.Waapi.JsonClient client)
        {
            SwitchCollection switchCollection = new SwitchCollection();

            try
            {
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
                    if (token["type"].ToString() == "SwitchContainer")
                    {
                        var container = token.ToObject<SwitchContainer>();

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

                        foreach (var switchToken in containerResults["return"])
                        {
                            Console.WriteLine("Adding child " + switchToken["name"] + " to " + container.name);
                            container.children.Add(switchToken.ToObject<SwitchContainerChild>());
                        }
                        containers.Add(container);
                    }

                    if (token["type"].ToString() == "SwitchGroup")
                    {
                        var group = token.ToObject<SwitchGroup>();

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

                        foreach (var switchToken in groupResults["return"])
                        {
                            Console.WriteLine("Adding switch " + switchToken["name"] + " to " + group.name);
                            group.switches.Add(switchToken.ToObject<WwiseSwitch>());
                        }
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
