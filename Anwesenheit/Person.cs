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
                string[] names = value.Split(new string[] { " " }, StringSplitOptions.None);
                if (names.Length > 0)
                {
                    _firstname = names[0];
                    if (names.Length > 1)
                    {
                        _lastname = names[1];
                    }
                }

            }
        }

        public string Émail { get; set; }
        public bool Present { get; set; }
        public bool Active { get; set; }
        public int Id { get; set; }

        public int Cardnr { get; set; }
        public string AbsenceReason { get; set; }
        public string Dayprog { get; set; }

        public string Birthday { get; set; }
    }
}
