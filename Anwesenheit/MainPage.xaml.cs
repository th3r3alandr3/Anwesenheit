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

namespace Anwesenheit
{
    public sealed partial class MainPage : Page
    {
        private readonly Dictionary<int, bool> state = new Dictionary<int, bool>();
        private readonly int[] excludedIds = new int[]{25, 26, 27, 32, 33, 70};
        private string jsonUrL = "";
        public MainPage()
        {

            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(300, 744));
            Init();
        }

        private async Task Init()
        {
            jsonUrL = await GetURLAsync();
            Person[] persons = GetPersons(jsonUrL);
            int defaultHeight = 0;
            foreach (Person person in persons)
            {
                state.Add(person.Cardnr, person.Present);
                UpdatetItemByPerson(person);
                if (!excludedIds.Contains(person.Cardnr))
                {
                    defaultHeight += 62;
                }
            }
            ApplicationView.PreferredLaunchViewSize = new Size(300, defaultHeight);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Timer timer = new Timer(CheckStates);
            timer.Start();
        }

        public void CheckStates()
        {
            Person[] persons = GetPersons(jsonUrL);
            foreach (Person person in persons)
            {
                if(state.ContainsKey(person.Cardnr) && state[person.Cardnr] != person.Present)
                {
                    state[person.Cardnr] = person.Present;
                    UpdatetItemByPerson(person);
                    ShowToast("Status Update", String.Format("{0} ist jetzt {1}", person.Name, person.Present ? "Anwesend" : "Abwesend"));
                }
            }
                
        }

        private Person[] GetPersons(string jsonUrL)
        {
            string json = new WebClient().DownloadString(jsonUrL);
            Person[] persons = JsonConvert.DeserializeObject<Person[]>(json);
            Array.Sort<Person>(persons, new Comparison<Person>((p1, p2) => p1.Name.CompareTo(p2.Name)));

            return persons;

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
            if (!excludedIds.Contains(person.Cardnr))
            {
                try
                {
                    Brush red = new SolidColorBrush(Color.FromArgb(255, 255, 75, 75));
                Brush green = new SolidColorBrush(Color.FromArgb(255, 75, 255, 75));
                Brush yellow = new SolidColorBrush(Color.FromArgb(255, 255, 255, 75));
                Dictionary<string, string> icons = new Dictionary<string, string>
                {
                    { "Schule", "🎓" },
                    { "Krankheit", "🚑" },
                    { "Urlaub", "✈" },
                    { "Elternzeit", "👨‍👩‍👦" },
                };

                    string absenceReason = person.AbsenceReason != null && person.AbsenceReason.Length > 0 ? person.AbsenceReason : person.Dayprog != null && person.Dayprog == "Schule" ? "Schule" : "";
                    string additionalInfos = HasBirthday(person.getBirthday()) ? "🎂" : "";
                    absenceReason = absenceReason.Length > 0 ? icons[absenceReason] : "";


                ListViewItem listItem = (ListViewItem)MainListBox.FindName(person.Cardnr.ToString());
                if (listItem == null)
                {

                    ListViewItem item = new ListViewItem
                    {
                        Name = person.Cardnr.ToString(),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                        Background = person.AbsenceReason == null || person.AbsenceReason.Length == 0 && person.Dayprog == null || person.Dayprog != "Schule" ? person.Present ? green : red : yellow,
                        Content = String.Format("{0} {1} {2}", absenceReason, additionalInfos, person.Name)
                    };
                    MainListBox.Items.Add(item);
                }else
                {
                    listItem.Background = person.AbsenceReason.Length == 0 ? person.Present ? green : red : yellow;
                }
                }
                catch (Exception e)
                {
                    string error = e.Message;
                }
            }
        }

        private async Task<string> GetURLAsync()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            string savedURL = "";
            string fileName = "data";

            if (await DoesFileExistAsync(storageFolder, fileName))
            {
                StorageFile dataFile = await storageFolder.GetFileAsync(fileName);
                savedURL = await FileIO.ReadTextAsync(dataFile);
            }
            else
            {

                string userInputUrl = await AskForUrl();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile dataFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
                await FileIO.WriteTextAsync(dataFile, userInputUrl);
                savedURL = userInputUrl;
            }

            return savedURL;
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


        private async Task<string> AskForUrl()
        {
            string userInputURL = "";
            string title = "URL";
            while (true)
            {
                userInputURL = await InputTextDialogAsync(title);
                if(await ValidUrl(userInputURL))
                {
                    break;
                }else
                {
                    title = "URL was invalid. Try again.";
                }
            }

            return userInputURL;
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

        private async Task<bool> ValidUrl(string url)
        {
            try
            {
                string json = await new WebClient().DownloadStringTaskAsync(url);
                Person[] persons = JsonConvert.DeserializeObject<Person[]>(json);
                return json.Length > 0;
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

            bool test = birthday.Day == now.Day && birthday.Month == now.Month;

            return test;
        }
    }
}
