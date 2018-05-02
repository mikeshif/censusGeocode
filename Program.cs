using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace testGeoCodeAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            var sWatch = new Stopwatch();
            sWatch.Start();
            Task.Run(function: ()
                => GeocodeResultAsync()).GetAwaiter().GetResult();
            sWatch.Stop();
            TimeSpan ts = sWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

        /// <summary>
        /// Geocoding and returning a result as a string .
        /// </summary>
        /// <returns></returns>
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
                KeyValuePair<string, string>[] uriParameters = UriParameter();

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
                await RequestContent(client, uriContent);
            }
            catch (TaskCanceledException ex)
            {
#if DEBUG
                Console.WriteLine("{0} First exception caught.", ex);
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("{0} Second exception caught.", ex);
#endif
            }

        }

        private static async Task RequestContent(HttpClient client, MultipartFormDataContent uriContent)
        {
            var cts = new CancellationTokenSource();

            if (cts != null)
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
            var baseUri = "https://geocoding.geo.census.gov/geocoder/geographies/addressbatch";
            HttpResponseMessage result = await client.PostAsync(baseUri, uriContent).ConfigureAwait(false);
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
                    cts.Dispose();
                }
            }
        }

        private static KeyValuePair<string, string>[] UriParameter()
        {
            return new[]
            {
                new KeyValuePair<string, string>("benchmark", "Public_AR_Current"),
                new KeyValuePair<string, string>("vintage", "Current_Current"),
                new KeyValuePair<string, string>("returntype","locations")
            };
        }

        private static string headerGeo()
        {
            return "Unique ID" + "," + "Address" + "," + "Address Match" + "," + "Match Type" + "," +
                                    "Matched Address" + "," + "Coordinates" + "," +
                                    "Tiger Line ID" + "," + "Side" + "," + "State FIPS" + "," +
                                    "County FIPS" + "," + "TRACT" + "," + "BLOCK";
        }
    }
}
