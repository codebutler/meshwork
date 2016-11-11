//using System;
//using System.Globalization;
//using System.Resources;

//namespace FileFind
//{
//    public class TextCatalog
//    {
//        ResourceManager manager;

//        static TextCatalog ()
//        {
//            manager = ResourceManager.CreateFileBasedResourceManager (baseName, 
//                    resourceDir, 
//                    singResourceSet);
//        }
		
//        public static string GetString (string str)
//        {
//            return manager.GetString(str);	
//        }

//        public static string GetString (string str, params string[] args)
//        {
//            return String.Format(TextCatalog.GetString(str), args);
//        }
//    }
//}
