//using System;

//// THis is not in use. Currently the generator creates the this class.
//namespace CaliburnExtras
//{
//    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
//    public class CaliburnNotifyAttribute : Attribute
//    {
//        public CaliburnNotifyAttribute() { }
//        public CaliburnNotifyAttribute(params string[] notifyingFields ) { }
//    }

//    public class Magic
//    {
//        [CaliburnNotify("Hello", "Fix", "Me")]
//        private string fixMe;

//        [CaliburnNotify]
//        private string fixMe2;
//    }
//}