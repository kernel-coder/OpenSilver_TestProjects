using System;
using Virtuoso.Portable.Database;

namespace Virtuoso.Portable
{
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// WARNING - The TableSchema objects describe the format of the flat file caches.  This information is used to build the cache
    ///           files on the server and to parse them apart on the client.
    ///           
    ///           If you change any of these configurations - you must do two things.
    ///           
    ///           1.) Rebuild the file on the server for each environment that you publish.
    ///           2.) Trigger the corresponding cache to rebuild a new file on the client.
    ///           
    ///           To trigger the entire cache to rebuild, you can increase the version number that is part of that cache's 
    ///           IDatabaseWrapper.StorageName property.
    ///
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public class TableSchemaRepository
    {
        //Just to have 1 place to maintain/build TableSchemas...
        public static TableSchema GetSchema(ReferenceTableName name)  //maybe turn name into enum/const w/version?
        {
            if (name.Value == ReferenceTableName.CacheConfiguration)
            {
                return new TableSchema(ReferenceTableName.CacheConfiguration)
                    .AddDefinition("Name", 30)
                    .AddDefinition("Anchor", 22) //DateTime is 22
                    .AddDefinition("TotalRecords", 19) //Integer is 19
                    .AddDefinition("CacheLoadCompleted", 1)
                    .AddDefinition("LastUpdatedDate", 22) //DateTime is 22
                    .AddDefinition("Ticks", 19) //Integer is 19 - this is long though....
                    .EndSpec();
            }
            else if (name.Value == ReferenceTableName.AddressMapping)
            {
                return new TableSchema(ReferenceTableName.AddressMapping)
                    .AddDefinition("AddressMapKey", 19) //Integer is 19
                    .AddDefinition("CBSAHomeHealth", 5)
                    .AddDefinition("CBSAHomeHealthEffectiveFrom", 22) //DateTime is 22
                    .AddDefinition("CBSAHomeHealthEffectiveTo", 22) //DateTime is 22
                    .AddDefinition("CBSAHospice", 5)
                    .AddDefinition("CBSAHospiceEffectiveFrom", 22) //DateTime is 22
                    .AddDefinition("CBSAHospiceEffectiveTo", 22) //DateTime is 22
                    .AddDefinition("City", 50)
                    .AddDefinition("County", 50)
                    .AddDefinition("State", 2)
                    .AddDefinition("ZipCode", 5)
                    .EndSpec();
            }
            else if (name.Value == ReferenceTableName.ICDCategory)
            {
                return new TableSchema(name.Value)
                    .AddDefinition("ICDCategoryKey", 19) //Integer is 19
                    .AddDefinition("ICDParentCategoryKey", 19)
                    .AddDefinition("MinCode", 10)
                    .AddDefinition("MaxCode", 10)
                    .AddDefinition("ICDCategoryCode", 10)
                    .AddDefinition("ICDCategoryDescription", 256)
                    .AddDefinition("Version", 2) // valid values - 9 or 10
                    .AddDefinition("Diagnosis", 1)
                    .AddDefinition("DeletedDate", 22) //DateTime is 22
                    .EndSpec();
            }
            else if (
                (name.Value == ReferenceTableName.ICDCM9) ||
                (name.Value == ReferenceTableName.ICDCM10) ||
                (name.Value == ReferenceTableName.ICDPCS9) ||
                (name.Value == ReferenceTableName.ICDPCS10)
                )
            {
                //FullText
                //icdCode.Code.ToLower(), " ",
                //icdCode.Short.ToLower(), " ",
                //display_name.ToLower())

                //var tableName = (name.Value == ReferenceTableName.ICDCM9) ? ReferenceTableName.ICDCM9 : ReferenceTableName.ICDCM10;
                return new TableSchema(name.Value)
                    .AddDefinition("ICDCodeKey", 19) //Integer is 19
                    //.AddDefinition("DisplayName", 100)  //not currently used in live...so making this small
                    .AddDefinition("Code", 8)
                    .AddDefinition("Version", 2) //even though this is an integer field, it will only ever have the values 9 or 10
                    .AddDefinition("Short", 256) //defined as nvarchar(max), however longest field is 201
                    .AddDefinition("EffectiveFrom", 22)
                    .AddDefinition("EffectiveThru", 22)
                    .AddDefinition("GEMSCount", 10)   //shouldn't need 
                    .AddDefinition("Diagnosis", 1)
                    .AddDefinition("RequiresAdditionalDigit", 1)
                    .AddDefinition("PDGMClinicalGroup", 40) 
                    .AddDefinition("PDGMComorbidityGroup", 40) 
                    .EndSpec();
            }
            else if ((name.Value == ReferenceTableName.ICDGEMS9) || (name.Value == ReferenceTableName.ICDGEMS10))
            {
                //var tableName = (name.Value == ReferenceTableName.ICDGEMS9) ? ReferenceTableName.ICDGEMS9 : ReferenceTableName.ICDGEMS10;
                return new TableSchema(name.Value)
                    .AddDefinition("ICDGEMSKey", 19) //Integer is 19
                    .AddDefinition("Version", 2)     //smallint - but will only ever have the values 9 and 10
                    .AddDefinition("Code9", 8)       //nvarchar(8)
                    .AddDefinition("Code10", 8)      //nvarhcar(8)
                    .AddDefinition("Short9", 100)    //nvarchar(max) - longest database value is 24
                    .AddDefinition("Short10", 100)   //nvarchar(max) - longest database value is 60
                    .AddDefinition("ApproximateFlag", 1)
                    .AddDefinition("NoMapFlag", 1)
                    .AddDefinition("CombinationFlag", 1)
                    .AddDefinition("Scenario", 2)     //values 0 - 6
                    .AddDefinition("ChoiceList", 2)   //values 0 - 4
                    .EndSpec();
            }
            else if (name.Value == ReferenceTableName.Allergy)
            {
                //FullText
                //allergyCode.UNII.GetValueOrDefault().ToLower(), " ",
                //allergyCode.SubstanceName.GetValueOrDefault().ToLower(), " ",
                //allergyCode.PreferredSubstanceName.GetValueOrDefault().ToLower(), " ",
                //display_name.ToLower()

                return new TableSchema(ReferenceTableName.Allergy)
                    .AddDefinition("AllergyCodeKey", 19)
                    //.AddDefinition("DisplayName", 256)
                    .AddDefinition("UNII", 20)
                    .AddDefinition("SubstanceName", 256)
                    .AddDefinition("PreferredSubstanceName", 256)
                    .AddDefinition("EffectiveFrom", 22)
                    .AddDefinition("EffectiveThru", 22)
                    //.AddDefinition("FullText", 20+256+256+256+50)
                    .EndSpec();
            }
            else if (name.Value == ReferenceTableName.MediSpanMedication)
            {
                return new TableSchema(ReferenceTableName.MediSpanMedication)
                    .AddDefinition("MedKey", 19)
                    .AddDefinition("DDID", 19)
                    .AddDefinition("RDID", 19)
                    .AddDefinition("MedType", 19)
                    .AddDefinition("Name", 100)
                    .AddDefinition("MedUnit", 100)
                    .AddDefinition("Route", 100)
                    .AddDefinition("RXType", 19)
                    .AddDefinition("MedNarcotic", 1)
                    .AddDefinition("ODate", 22)
                    .EndSpec();
            }
            else
                throw new ArgumentOutOfRangeException("name", "Invalid value");
        }
    }
}
