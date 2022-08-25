#region Usings

using Virtuoso.Portable.Database;

#endregion

namespace Virtuoso.Core.Cache.ICD
{
    public static class ICDCategoryExtensions
    {
        public static void RecordSetToICDCategory(RecordSet recordSet, Server.Data.ICDCategory entity)
        {
#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.ICDCategoryKey = recordSet.GetInteger("ICDCategoryKey", throwExceptionIfColumnNotExists);
            entity.ICDParentCategoryKey =
                recordSet.GetNullableInteger("ICDParentCategoryKey", throwExceptionIfColumnNotExists);
            entity.MinCode = recordSet.GetString("MinCode", throwExceptionIfColumnNotExists);
            entity.MaxCode = recordSet.GetString("MaxCode", throwExceptionIfColumnNotExists);
            entity.ICDCategoryCode = recordSet.GetString("ICDCategoryCode", throwExceptionIfColumnNotExists);
            entity.ICDCategoryDescription =
                recordSet.GetString("ICDCategoryDescription", throwExceptionIfColumnNotExists);
            entity.Version = recordSet.GetInteger("Version", throwExceptionIfColumnNotExists);
            entity.Diagnosis = recordSet.GetBoolean("Diagnosis", throwExceptionIfColumnNotExists);
            entity.DeletedDate = recordSet.GetNullableDateTime("DeletedDate", throwExceptionIfColumnNotExists);
        }
    }
}