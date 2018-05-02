using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace testGeoCodeAPI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sWatch = new Stopwatch();
            sWatch.Start();
            //bool contLooping = true;

            //while (contLooping)
            //{
            // Start a task 
            // block the main Console thread until your asynchronous work has completed.

            Task.Run(function: ()
                => GeocodeResultAsync()).GetAwaiter().GetResult();
            sWatch.Stop();
            TimeSpan ts = sWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
            //Console.WriteLine(value: sWatch.Elapsed);
            //await GeocodeResultAsync();              
            //await GeocodeResultAsync(); //.GetAwaiter().GetResult();
            //if (sWatch.ElapsedMilliseconds > 80000)
            //{
            //    throw new TimeoutException();
            //}
            //  contLooping = false;
            //}

        }

        private static async Task GeocodeResultAsync()
        {
            string filePath = @"C:\\Users\\mikes\\Desktop\\Addresses.csv";
            try
            {
                HttpClient client = new HttpClient();

                var uriContent = new MultipartFormDataContent();


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                   | SecurityProtocolType.Tls11
                                   | SecurityProtocolType.Tls12
                                   | SecurityProtocolType.Ssl3;
                var uriParameters = new[]
                {
                        new KeyValuePair<string, string>("benchmark", "Public_AR_Current"),
                        new KeyValuePair<string, string>("vintage", "Current_Current"),
                        new KeyValuePair<string, string>("returntype","locations")
                        };

                foreach (var keyValuePair in uriParameters)
                {
                    uriContent.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                }
                var addressFileContent = new ByteArrayContent(content: File.ReadAllBytes(filePath));
                addressFileContent.Headers.ContentDisposition =
                        new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "addressFile", // the missing piece
                                    FileName = "Addresses.csv", // The trick 
                                };
                uriContent.Add(addressFileContent);
                var cts = new CancellationTokenSource();

                if (cts != null)
                    cts.Cancel();
                cts = new CancellationTokenSource();

                cts.CancelAfter(millisecondsDelay: 300000); // 5 minutes
                var baseUri = "https://geocoding.geo.census.gov/geocoder/geographies/addressbatch";
                HttpResponseMessage result = await client.PostAsync(baseUri, uriContent).ConfigureAwait(false);

                //client.Timeout = TimeSpan.FromSeconds(300);
                string response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);


#if DEBUG
                Console.WriteLine(response);
#endif
                if (result.IsSuccessStatusCode)
                {

                    string Outputpath = @"C:\\Users\\mikes\\Desktop\\OutGeo.csv";
                    using (var csvWriter = new StreamWriter(Outputpath))
                    {
                        string geoCodeHeaders = headerGeo();
                        //File.WriteAllText(Outputpath, clientHeader);
                        //int i = 1;
                        //string output = String.Format("{0, -60}", geoCodeHeaders);
                        await csvWriter.WriteLineAsync(geoCodeHeaders);
                        await csvWriter.WriteLineAsync(response);
                        csvWriter.Dispose();
                        csvWriter.Close();
                        //cts.Dispose();
                    }
                }


            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine("{0} First exception caught.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Second exception caught.", ex);
            }

        }

        private static string headerGeo()
        {
            return "Unique ID" + "," + "Street" + "," + "City" + "," + "State" + "," +
                                    "Zip Code" + "," + "Address Match" + "," + "Match Type" + "," +
                                    "Matched Street" + "," + "Longitude" + "," + "Latitude" + "," +
                                    "Tiger Line ID" + "," + "Side" + "," + "State FIPS" + "," +
                                    "County FIPS" + "," + "TRACT" + "," + "BLOCK";
        }
    }
}
