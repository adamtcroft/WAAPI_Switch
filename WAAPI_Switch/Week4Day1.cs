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

                client.Close();
            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
