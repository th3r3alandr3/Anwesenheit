using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anwesenheit
{
    class Person
    {
        private string _firstname = "";
        private string _lastname = "";

        public string Name
        {
            get => String.Format("{0} {1}", _firstname, _lastname);
            set
            {
                string[] names = value.Split(new string[] { ", " }, StringSplitOptions.None);
                if (names.Length > 0)
                {
                    _lastname = names[0];
                    if (names.Length > 1)
                    {
                        _firstname = names[1];
                    }
                }

            }
        }

        private DateTime _birthday = new DateTime();
        public string Birthday
        {
            get => String.Format("{0}.{1}.{2}", _birthday.Day, _birthday.Month, _birthday.Year);
            set
            {
                if (value.Length > 0)
                {
                    _birthday = DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                }
            }

        }
        public int Cardnr { get; set; }
        public bool Present { get; set; }
        public string Dayprog { get; set; }
        public string AbsenceReason { get; set; }
    }
}
