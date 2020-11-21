using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ViewManagement;
using aes;

namespace Anwesenheit
{
    public sealed partial class MainPage : Page
    {
        private static string password = "gzE9et7#a8}X#-r~";
        private readonly Dictionary<int, bool> state = new Dictionary<int, bool>();
        private readonly int[] excludedIds = new int[] { 135363 };
        private Person[] persons;
        private ClockodoAPI clockodoAPI;
        private Dictionary<int, int> absences;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(300, 744));
            Init();
        }

        private async Task Init()
        {
            this.clockodoAPI = await GetClockodoAPI();
            var persons = await this.clockodoAPI.getUsers();

            int defaultHeight = 0;
            foreach (Person person in persons)
            {
                state.Add(person.Id, person.Present);
                UpdatetItemByPerson(person);
                if (person.Active && !excludedIds.Contains(person.Id))
                {
                    defaultHeight += 62;
                }
            }
            ApplicationView.PreferredLaunchViewSize = new Size(300, defaultHeight);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            this.absences = await this.clockodoAPI.GetAbsences();
            this.CheckStatesAsync();
            Timer timer = new Timer(this.CheckStatesAsync);
            timer.Start();

            Timer absenceTimer = new Timer(this.CheckAbsencesAsync, 3600);
            absenceTimer.Start();
        }

        public async void CheckStatesAsync()
        {
            var newstate = await this.clockodoAPI.GetEntires(this.state);
            var persons = await this.clockodoAPI.getUsers();
            foreach (Person person in persons)
            {
                if (person.Active && !excludedIds.Contains(person.Id) && state.ContainsKey(person.Id))
                {
                        person.Present = newstate[person.Id];
                        if (state[person.Id] != person.Present)
                        {
                            state[person.Id] = person.Present;
                            UpdatetItemByPerson(person);
                            ShowToast("Status Update", String.Format("{0} ist jetzt {1}", person.Name, person.Present ? "Anwesend" : "Abwesend"));
                        }
                }
            }

        }

        public async void CheckAbsencesAsync()
        {
            this.absences = await this.clockodoAPI.GetAbsences();
        }

        private static void ShowToast(string title, string content)
        {
            XmlDocument toastXml = new XmlDocument();
            string xml = $@"
              <toast activationType='foreground'>
              <visual>
                <binding template='ToastGeneric'>
                 <text>{title}</text>
                 <text>{content}</text>
                </binding>
               </visual>
              </toast>";
            toastXml.LoadXml(xml);
            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void UpdatetItemByPerson(Person person)
        {
            if (person.Active && !excludedIds.Contains(person.Id))
            {
                try
                {
                    Brush red = new SolidColorBrush(Color.FromArgb(255, 255, 75, 75));
                    Brush green = new SolidColorBrush(Color.FromArgb(255, 75, 255, 75));
                    Brush yellow = new SolidColorBrush(Color.FromArgb(255, 255, 255, 75));
                    Dictionary<int, string> icons = new Dictionary<int, string>
                    {
                        { 1, "✈" },
                        { 2, "✈" },
                        { 3, "✈" },
                        { 4, "🚑" },
                        { 5, "🚑" },
                        { 6, "🎓" },                     
                        { 7, "👨‍👩‍👦" },
                        { 8, "🏠" },
                        { 9, "🏠" },
                    };

                    string absenceReason = "";

                    if (this.absences != null)
                    {
                        absenceReason = this.absences.ContainsKey(person.Id) ? icons[this.absences[person.Id]] : "";
                    }


                    ListViewItem listItem = (ListViewItem)MainListBox.FindName(person.Id.ToString());
                    if (listItem == null)
                    {

                        ListViewItem item = new ListViewItem
                        {
                            Name = person.Id.ToString(),
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                            Background = absenceReason.Length <= 0 ? person.Present ? green : red : yellow,
                            Content = String.Format("{0} {1}", absenceReason, person.Name)
                        };
                        MainListBox.Items.Add(item);
                    }
                    else
                    {
                        listItem.Background = absenceReason.Length <= 0 ? person.Present ? green : red : yellow;
                    }
                }
                catch (Exception e)
                {
                    string error = e.Message;
                }
            }
        }

        private async Task<ClockodoAPI> GetClockodoAPI()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            string data = "";
            string fileName = "data";
            if (await DoesFileExistAsync(storageFolder, fileName))
            {
                StorageFile dataFile = await storageFolder.GetFileAsync(fileName);
                string jsonString = Cryptography.Decrypt(await FileIO.ReadTextAsync(dataFile), password);
                ClockodoAPI clockodoApi = JsonConvert.DeserializeObject<ClockodoAPI>(jsonString);

                return clockodoApi;

           }
            else
            {
                string userInput = await AskForCredentials();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile dataFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(dataFile, Cryptography.Encrypt(userInput, password));

                ClockodoAPI clockodoApi = JsonConvert.DeserializeObject<ClockodoAPI>(userInput);

                return clockodoApi;
            }
        }

        private async Task<bool> DoesFileExistAsync(StorageFolder storageFolder, string fileName)
        {
            try
            {
                await storageFolder.GetFileAsync(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }


        private async Task<string> AskForCredentials()
        {
            var userInputKey = "";
            var userInputMail = "";
            var titleKey = "API Key";
            var titleMail = "Mail";

            while (true)
            {
                userInputKey = await InputTextDialogAsync(titleKey);
                userInputMail = await InputTextDialogAsync(titleMail);

                if (await validateAPI(userInputKey, userInputMail))
                {
                    break;
                }
                else
                {
                    titleKey = "API Key was invalid. Try again.";
                    titleMail = "Mail was invalid. Try again.";
                }
            }

            var credentials = new Dictionary<string, string>()
            {
                { "Mail", userInputMail },
                { "ApiKey", userInputKey },
            };

            return JsonConvert.SerializeObject(credentials);
        }

        private async Task<string> InputTextDialogAsync(string title)
        {
            TextBox inputTextBox = new TextBox();
            inputTextBox.AcceptsReturn = false;
            inputTextBox.Height = 32;
            ContentDialog dialog = new ContentDialog();
            dialog.Content = inputTextBox;
            dialog.Title = title;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Ok";
            dialog.SecondaryButtonText = "Cancel";
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                return inputTextBox.Text;
            else
                return "";
        }

        private async Task<bool> validateAPI(string userInputKey, string userInputMail)
        {
            try
            {
                ClockodoAPI clockodoAPI = new ClockodoAPI(userInputKey, userInputMail);
                clockodoAPI.getUsers();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void MainListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ListViewItem item = (ListViewItem)MainListBox.SelectedItem;
            if (item != null)
            {
                item.IsSelected = false;
            }

        }

        public bool HasBirthday(DateTime birthday)
        {
            DateTime now = DateTime.Today;

            return birthday.Day == now.Day && birthday.Month == now.Month;
        }
    }
}
