using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace WAAPI_Switch
{
    class MainClassDay1
    {
        public static void MainDay1(string[] args)
        {
            _MainDay1().Wait();
        }

        static async Task _MainDay1()
        {
            AK.Wwise.Waapi.JsonClient client = new AK.Wwise.Waapi.JsonClient();

            await client.Connect();

            client.Disconnected += () =>
            {
                System.Console.WriteLine("We lost connection!");
            };

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

            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
