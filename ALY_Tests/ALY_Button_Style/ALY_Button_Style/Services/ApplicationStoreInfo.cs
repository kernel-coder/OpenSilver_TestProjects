//using System;
//using System.IO;
//using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Shapes;
//using System.ComponentModel.Composition;

//namespace Virtuoso.Core.Services
//{
//    //[Export]
//    public class ApplicationStoreInfo
//    {
//        private readonly string ApplicationName = @"ClinicalVirtuoso";
//        private string ApplicationID;
//        private string ApplicationStore;  //disk location for application storage

//        //[Import]
//        //public VirtuosoApplicationConfiguration Configuration { get; set; }

//        public string ApplicationStoreSubFolder { get; set; }
//        public Environment.SpecialFolder ApplicationStoreSpecialFolder { get; set; }

//        public ApplicationStoreInfo()
//        {
//            //CompositionInitializer.SatisfyImports(this);
//            var BaseUri = System.Windows.Application.Current.Host.Source;
//            string tenantRoot = String.Empty;

//            //determine the virtual application name from the BaseUri
//            var virAppName = BaseUri.AbsolutePath.Substring(0, BaseUri.AbsolutePath.LastIndexOf("/ClientBin/Virtuoso.xap"));
//            if (String.IsNullOrEmpty(virAppName))
//            {
//                //MessageBox.Show("local");
//                tenantRoot = "local";  //only true when there is no virtual application - e.g. developers local workstations
//            }
//            else
//            {
//                virAppName = virAppName.Substring(1);
//                //MessageBox.Show(virAppName);
//                tenantRoot = virAppName;
//            }

//            //ApplicationID = String.Format("{0}", Configuration.TenantName);  //JWE - think I would prefer a guid here...
//            ApplicationID = String.Format("{0}", tenantRoot);

//            ApplicationStoreSpecialFolder = Environment.SpecialFolder.MyDocuments;

//            ApplicationStoreSubFolder = System.IO.Path.Combine(ApplicationName, ApplicationID);

//            ApplicationStore = System.IO.Path.Combine(
//                Environment.GetFolderPath(ApplicationStoreSpecialFolder),
//                ApplicationName,
//                ApplicationID);

//            if (Directory.Exists(ApplicationStore) == false)
//                Directory.CreateDirectory(ApplicationStore); //create all directories and subdirectories
//        }

//        public string GetUserStoreForApplication(string subFolder = null)
//        {
//            if (String.IsNullOrEmpty(subFolder))
//                return ApplicationStore;
//            else
//            {
//                var subFolderPath = System.IO.Path.Combine(ApplicationStore, subFolder);

//                if (Directory.Exists(subFolderPath) == false)
//                    Directory.CreateDirectory(subFolderPath); //create all directories and subdirectories

//                return subFolderPath;
//            }
//        }
//    }
//}
