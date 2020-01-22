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
            // Connect/Disconnect Handler
            AK.Wwise.Waapi.JsonClient client = new AK.Wwise.Waapi.JsonClient();

            await client.Connect();

            client.Disconnected += () =>
            {
                System.Console.WriteLine("We lost connection!");
            };

            // Build query and options for getting all Switch Containers and Switch Groups in project
            var query = new JObject
                        (
                        new JProperty("from", new JObject
                            (
                                new JProperty("ofType", new JArray(new string[] { "SwitchContainer", "SwitchGroup" }))
                            ))
                        );

            var options = new JObject(
                new JProperty("return", new string[] { "name", "id", "type" }));

            // try/catch
            try
            {
                // WAAPI object.get call
                var results = await client.Call(
                    ak.wwise.core.@object.get,
                    query,
                options);
                Console.WriteLine(results);
                Console.WriteLine();


                // Initial JSON sort
                var tokens = results["return"];

                // Create two lists of custom classes that match switch object types
                List<SwitchContainer> containers = new List<SwitchContainer>();
                List<SwitchGroup> groups = new List<SwitchGroup>();

                // Sort the JSON into SwitchContainer and SwitchGroup objects
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
                Console.WriteLine();

                // LINQ Query - this turns "groups" into a list of groups where you have a
                // Switch Container with a matching name
                groups = groups.Where(group => containers.Any(container => container.name == group.name)).ToList();

                // Write out all the containers
                Console.WriteLine("Containers:");
                foreach (var container in containers)
                    Console.WriteLine(container.name);

                // Write out all the groups
                Console.WriteLine();
                Console.WriteLine("Groups:");
                foreach (var group in groups)
                    Console.WriteLine(group.name);

                Console.WriteLine();

                // Close the connection
                await client.Close();
            }
            catch (AK.Wwise.Waapi.Wamp.ErrorException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
