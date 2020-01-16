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
        public static void Main(string[] args)
        {
            _Main().Wait();
        }

        static async Task _Main()
        {
            var client = CreateConnection().Result;
            var switches = GetSwitchObjects(client).Result;
            await AssignSwitchContainers(client, switches);
        }

        private static async Task AssignSwitchContainers(AK.Wwise.Waapi.JsonClient client, List<JToken>[] switches)
        {
            foreach (var container in switches[0])
            {
                var result = await client.Call(
                    ak.wwise.core.@object.get,
                    new JObject
                    (
                       new JProperty("from", new JObject(new JProperty("id", new JArray(new string[] { container["id"].ToString() }))))
                    ),
                    new JObject
                    (
                        new JProperty("return", new string[] { "@SwitchGroupOrStateGroup" })
                    ));

                var token = result["return"][0]["@SwitchGroupOrStateGroup"];

                if (token["id"].ToString() == "{00000000-0000-0000-0000-000000000000}")
                {
                    foreach (var group in switches[1])
                    {
                        if (container["name"].ToString() == group["name"].ToString())
                        {
                            Console.WriteLine("Assigning " + container["name"].ToString() + " to " + group["name"].ToString());
                            await client.Call(
                                ak.wwise.core.@object.setReference,
                                new JObject
                                (
                                    new JProperty("reference", "SwitchGroupOrStateGroup"),
                                    new JProperty("object", container["id"].ToString()),
                                    new JProperty("value", group["id"].ToString())
                                ),
                                null
                            );
                        }
                    }
                }
            }
        }

        private static async Task<List<JToken>[]> GetSwitchObjects(AK.Wwise.Waapi.JsonClient client)
        {
            List<JToken>[] switches = new List<JToken>[2];

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

                List<JToken> containers = new List<JToken>();
                List<JToken> containerNames = new List<JToken>();
                List<JToken> groups = new List<JToken>();
                List<JToken> groupNames = new List<JToken>();

                foreach (var token in tokens)
                {
                    if (token["type"].ToString() == "SwitchContainer")
                    {
                        containers.Add(token);
                        containerNames.Add(token["name"]);
                        Console.WriteLine("Added " + token["name"].ToString() + " to Containers!");
                    }

                    if (token["type"].ToString() == "SwitchGroup")
                    {
                        groups.Add(token);
                        groupNames.Add(token["name"]);
                        Console.WriteLine("Added " + token["name"].ToString() + " to Groups!");
                    }
                }

                var differences = groupNames.Except(containerNames);
                List<JToken> toRemove = new List<JToken>();

                Console.WriteLine("Matching Groups: ");
                foreach (var group in groups)
                {
                    if (differences.Contains(group["name"]))
                        toRemove.Add(group);
                    else
                        Console.WriteLine(group["name"]);
                }

                groups = groups.Except(toRemove).ToList();

                switches[0] = containers;
                switches[1] = groups;
            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }

            return switches;
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
