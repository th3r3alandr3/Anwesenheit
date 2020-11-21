using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Globalization;

namespace Anwesenheit
{
    class ClockodoAPI
    {
        public string ApiKey { get; set; }
        public string Mail { get; set; }

        public ClockodoAPI(string apiKey, string mail)
        {
            this.ApiKey = apiKey;
            this.Mail = mail;
        }

        public async Task<Person[]> getUsers()
        {
            return await this.GetPersons(await this.request("https://my.clockodo.com/api/users"));
        }

        private async Task<string> request(string url)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            var headers = httpClient.DefaultRequestHeaders;
            headers.Add("X-ClockodoApiUser", this.Mail);
            headers.Add("X-ClockodoApiKey", this.ApiKey);

            string header = "ie";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }

            header = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }

            Uri requestUri = new Uri(url);

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                return await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            return "";
        }

        private async Task<Person[]> GetPersons(string json)
        {
            var users = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json)["users"];
            Person[] persons = JsonConvert.DeserializeObject<Person[]>(JsonConvert.SerializeObject(users));
            Array.Sort<Person>(persons, new Comparison<Person>((p1, p2) => p1.Name.CompareTo(p2.Name)));

            return persons;
        }

        public async Task<Dictionary<int, bool>> GetEntires(Dictionary<int, bool> states)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            string dateFormat = "yyyy-MM-dd HH:mm:ss";

            var now = DateTime.Now;
            queryString.Add("time_since", new DateTime(now.Year, now.Month, now.Day, 0, 0, 1).ToString(dateFormat));
            queryString.Add("time_until", DateTime.Now.Add(new TimeSpan(0, 5, 0)).ToString(dateFormat));
            

            string url = string.Format("{0}?{1}", "https://my.clockodo.com/api/entries", queryString.ToString());
            Dictionary<string, dynamic> entries = new Dictionary<string, dynamic>();
            string json = null;

            json = await request(url);
            entries = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

            states = states.ToDictionary(p => p.Key, p => false);

            if (entries.ContainsKey("entries"))
            {
                foreach (var entry in entries["entries"])
                {
                    if (entry.ContainsKey("is_clocking") && entry.ContainsKey("users_id"))
                    {
                        bool isClocking = Convert.ToBoolean(entry["is_clocking"]);
                        int id = Convert.ToInt32(entry["users_id"]);

                        if (isClocking)
                        {
                            if (states.ContainsKey(id))
                            {
                                states[id] = true;
                            }
                            else
                            {
                                states.Add(id, true);
                            }
                        }
                    }
                }
            }
            return states;
        }

        public async Task<Dictionary<int, int>> GetAbsences()
        {
            var absencesDict = new Dictionary<int, int>();

            string year = DateTime.Now.Year.ToString();
            string url = string.Format("{0}?year={1}", "https://my.clockodo.com/api/absences", year);
            string json = await request(url);
            var absences = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

            if (absences.ContainsKey("absences"))
            {
                foreach (var absence in absences["absences"])
                {
                    var dateSince = DateTime.ParseExact(absence["date_since"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var dateUntil = DateTime.ParseExact(absence["date_until"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    if (today.Ticks > dateSince.Ticks && today.Ticks < dateUntil.Ticks)
                    {
                        absencesDict[absence["users_id"]] = absence["type"];
                    }
                }
            }


            return absencesDict;
        }
    }
}
