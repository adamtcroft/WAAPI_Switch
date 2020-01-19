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

        static async Task _MainDay2()
        {
            var client = CreateConnection().Result;
            var switches = GetSwitchObjects(client).Result;
            await AssignSwitchContainers(client, switches);
            client.Close();
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

        private static async Task<SwitchCollection> GetSwitchObjects(AK.Wwise.Waapi.JsonClient client)
        {
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
