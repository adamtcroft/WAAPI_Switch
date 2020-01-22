using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace WAAPI_Switch
{
    class MainClassDay2
    {
        public static void MainDay2(string[] args)
        {
            _MainDay2().Wait();
        }

        // _Main function is now changed up to call other functions
        // This is intended to break up the code and aid readability
        static async Task _MainDay2()
        {
            // .Result is used with an async function where you want a returned value
            var client = CreateConnection().Result;
            var switches = GetSwitchObjects(client).Result;

            // A returned value isn't required here
            await AssignSwitchContainers(client, switches);
            await client.Close();
        }

        // This is a function to assign the proper group to each Switch Container based on name
        private static async Task AssignSwitchContainers(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            // Loop through every Switch Container in the collection
            foreach (var container in switches.containers)
            {
                // Get the Switch Container's current switch/state group assignment
                // This will come back as 00000000-0000-0000-0000-000000000000} if there is no assignment
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

                // Parsing the result down to only the JSON information needed
                var token = result["return"][0]["@SwitchGroupOrStateGroup"];

                // Check to see if the group isn't assigned or if the assigned group isn't in the list of groups
                // If the assigned group isn't in the list of groups, its name doesn't match
                if (token["id"].ToString() == "{00000000-0000-0000-0000-000000000000}"
                    || token["id"].ToString() != switches.groups.Find(group => group.name == container.name).name)
                {
                    // Loop through the groups (you could also use a LINQ query here to pull out groups where
                    // the group and container names match)
                    foreach (var group in switches.groups)
                    {
                        // If the group and container names match, assign the group to the container with WAAPI
                        if (container.name == group.name)
                        {
                            Console.WriteLine("Assigning " + container.name + " to " + group.name);
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

        // This is the _Main function from Day 1 with one significant change - a returned object
        private static async Task<SwitchCollection> GetSwitchObjects(AK.Wwise.Waapi.JsonClient client)
        {
            // "SwitchCollection" is a custom object that houses a list of SwitchContainers and a list
            // of SwitchGroups - this object carries all the queried data from Wwise
            SwitchCollection switchCollection = new SwitchCollection();

            var query = new JObject
                        (
                        new JProperty("from", new JObject
                            (
                                new JProperty("ofType", new JArray(new string[] { "SwitchContainer", "SwitchGroup" }))
                            ))
                        );

            var options = new JObject(
                new JProperty("return", new string[] { "name", "id", "type" }));

            try
            {
                var results = await client.Call(
                    ak.wwise.core.@object.get,
                    query,
                options);
                Console.WriteLine(results);

                var tokens = results["return"];

                List<SwitchContainer> containers = new List<SwitchContainer>();
                List<SwitchGroup> groups = new List<SwitchGroup>();

                Console.WriteLine();
                foreach (var token in tokens)
                {
                    if (token["type"].ToString() == "SwitchContainer")
                    {
                        containers.Add(token.ToObject<SwitchContainer>());
                        Console.WriteLine("Added " + token["name"].ToString() + " to Containers!");
                    }

                    if (token["type"].ToString() == "SwitchGroup")
                    {
                        groups.Add(token.ToObject<SwitchGroup>());
                        Console.WriteLine("Added " + token["name"].ToString() + " to Groups!");
                    }
                }

                groups = groups.Where(group => containers.Any(container => container.name == group.name)).ToList();

                Console.WriteLine();
                Console.WriteLine("Containers:");
                foreach (var container in containers)
                    Console.WriteLine(container.name);

                Console.WriteLine();
                Console.WriteLine("Groups:");
                foreach (var group in groups)
                    Console.WriteLine(group.name);

                // Add the container and group lists to the SwitchCollection object
                switchCollection.containers = containers;
                switchCollection.groups = groups;
            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }

            // Give the SwitchCollection object back to the new _Main function
            return switchCollection;
        }

        // Simple function to house our connect/disconnect handlers
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
