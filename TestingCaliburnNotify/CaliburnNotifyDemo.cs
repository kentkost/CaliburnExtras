using System;
using System.ComponentModel;
using CaliburnExtras;
using Caliburn.Micro;

namespace TestingCaliburnNotify
{
    partial class MagicBall : Screen
    {
        [CaliburnNotify]
        int position = -2; //in lightyears
        [CaliburnNotify]
        int size = 9000; //in centimeters
        [CaliburnNotify("Size")]
        string name = "Kevin"; //in english

        string lastName = "Mahoney";
        public string LastName
        {
            get { return lastName; }
            set 
            { 
                if(lastName == value) { return; }   
                lastName = value;
                NotifyOfPropertyChange(() => LastName);
            }
        }
    }


    class CaliburnNotifyDemo
    {
        public static void DoSomething()
        {
            MagicBall kevin = new MagicBall();

            kevin.PropertyChanged += Print;

            kevin.Name = "Kevin 2";
            kevin.Position += 5;
            kevin.Size = kevin.GetHashCode();
        }

        public static void Print(object obj, PropertyChangedEventArgs args)
        {
            Console.WriteLine(args.PropertyName + ": " + obj);
        }
    }
}
