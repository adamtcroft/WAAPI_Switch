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
            client.Close();
        }

        private static async Task GetSwitchAssignments(AK.Wwise.Waapi.JsonClient client, SwitchCollection switches)
        {
            var result = await client.Call(
                ak.wwise.core.switchContainer.getAssignments,
                new JObject
                (
                    new JProperty("id", switches.containers.ElementAt(0).id)
                ),
                null
                );

            var token = result["return"][0];

            Console.WriteLine();
            Console.WriteLine("Assignments: ");
            Console.WriteLine(token);
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

                Console.WriteLine();
                Console.WriteLine("Matching Filters and Groups:");
                foreach (var group in groups)
                {
                    Console.WriteLine(group.name);
                }

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
