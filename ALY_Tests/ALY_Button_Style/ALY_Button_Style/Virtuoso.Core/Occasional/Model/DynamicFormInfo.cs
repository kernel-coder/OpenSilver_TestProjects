#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Virtuoso.Serializer;
using Virtuoso.Serializer.Extensions;

#endregion

namespace Virtuoso.Core.Occasional.Model
{
    public interface IDynamicFormInfo : ICustomBinarySerializable
    {
        int EncounterKey { get; set; }
        bool IsValid { get; }
    }

    public interface IObjectVersion
    {
        string Version { get; set; }
    }

    public class DynamicFormInfo : IDynamicFormInfo, IObjectVersion
    {
        public DynamicFormInfo()
        {
            try
            {
                //NOTE: only the main Virtuoso project is versioned - via a build task - and only for RELEASE.
                var asm = Utility.AssemblyFileVersionInfo.GetAssemblyByName("Virtuoso, Version=");
                var _assemblyFileVersionInfo = new Utility.AssemblyFileVersionInfo(asm);
                Version = _assemblyFileVersionInfo.Version; //"1.1.46.0"
            }
            catch (Exception)
            {
                Version = "0.0.0.0"; //should only hit this in a unit test
            }
        }

        public string Version { get; set; }

        public Guid HeaderGUID { get; set; }

        public Guid TrailerGUID { get; set; }

        private bool __IsValid;
        public bool IsValid => __IsValid;

        public int TaskKey { get; set; }
        public int ServiceTypeKey { get; set; }

        public int AdmissionKey { get; set; }
        public int FormKey { get; set; }

        public int PatientKey { get; set; }
        public int EncounterKey { get; set; }
        public DateTime? LastLocalSaveDate { get; set; }
        public int EncounterStatus { get; set; }
        public int PreviousEncounterStatus { get; set; }
        public string PreviousPreEvalStatus { get; set; }
        public string PatientName { get; set; }
        public string AddendumText { get; set; }
        public DynamicFormQuestionState SavedQuestionState { get; set; }

        #region ICustomBinarySerializable

        public void WriteDataTo(BinaryWriter _Writer)
        {
            HeaderGUID = TrailerGUID = Guid.NewGuid();

            _Writer.Write(HeaderGUID);

            _Writer.WriteStringNullable(Version);

            _Writer.Write(TaskKey);
            _Writer.Write(ServiceTypeKey);

            _Writer.Write(AdmissionKey);
            _Writer.Write(FormKey);

            _Writer.Write(PatientKey);
            _Writer.Write(EncounterKey);
            _Writer.Write(EncounterStatus);
            _Writer.Write(PreviousEncounterStatus);

            _Writer.WriteStringNullable(PreviousPreEvalStatus);
            _Writer.WriteStringNullable(PatientName);
            _Writer.WriteStringNullable(AddendumText);

            LastLocalSaveDate = DateTime.UtcNow.ToLocalTime();
            _Writer.Write(LastLocalSaveDate);

            if (SavedQuestionState == null)
            {
                _Writer.Write(-1);
            }
            else
            {
                _Writer.Write(1);
                SavedQuestionState.WriteDataTo(_Writer);
            }

            _Writer.Write(TrailerGUID);
        }

        public void SetDataFrom(BinaryReader _Reader)
        {
            try
            {
                HeaderGUID = _Reader.ReadGuid();

                Version = _Reader.ReadStringNullable();

                TaskKey = _Reader.ReadInt32();
                ServiceTypeKey = _Reader.ReadInt32();

                AdmissionKey = _Reader.ReadInt32();
                FormKey = _Reader.ReadInt32();

                PatientKey = _Reader.ReadInt32();
                EncounterKey = _Reader.ReadInt32();
                EncounterStatus = _Reader.ReadInt32();
                PreviousEncounterStatus = _Reader.ReadInt32();

                PreviousPreEvalStatus = _Reader.ReadStringNullable();
                PatientName = _Reader.ReadStringNullable();
                AddendumText = _Reader.ReadStringNullable();
                LastLocalSaveDate = _Reader.ReadNullableDateTime();

                var haveQuestions = _Reader.ReadInt32();
                if (haveQuestions > 0)
                {
                    SavedQuestionState = new DynamicFormQuestionState();
                    SavedQuestionState.SetDataFrom(_Reader);
                }

                TrailerGUID = _Reader.ReadGuid();

                if (HeaderGUID.Equals(TrailerGUID))
                {
                    __IsValid = true;
                }
                else
                {
                    __IsValid = false;
                }
            }
            catch (Exception)
            {
                __IsValid = false;
            }
        }

        public object GetKey()
        {
            return TaskKey;
        }

        public bool Ignore()
        {
            return false;
        }

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("TaskKey={0}", TaskKey);
            sb.AppendFormat(",ServiceTypeKey={0}", ServiceTypeKey);
            sb.AppendFormat(",AdmissionKey={0}", AdmissionKey);
            sb.AppendFormat(",FormKey={0}", FormKey);
            sb.AppendFormat(",PatientKey={0}", PatientKey);
            sb.AppendFormat(",EncounterKey={0}", EncounterKey);
            sb.AppendFormat(",LastLocalSaveDate={0}", LastLocalSaveDate);
            sb.AppendFormat(",EncounterStatus={0}", EncounterStatus);
            sb.AppendFormat(",PreviousEncounterStatus={0}", PreviousEncounterStatus);
            sb.AppendFormat(",PreviousPreEvalStatus={0}", PreviousPreEvalStatus);
            sb.AppendFormat(",PatientName={0}", PatientName);
            sb.AppendFormat(",AddendumText={0}", AddendumText);

            return sb.ToString();
        }
    }

    public class DynamicFormQuestionState : ICustomBinarySerializable
    {
        int Key { get; set; }

        public DynamicFormQuestionState()
        {
            SavedQuestions = new Dictionary<int, Dictionary<string, string>>();
        }

        public Dictionary<int, Dictionary<string, string>> SavedQuestions { get; set; }

        #region ICustomBinarySerializable

        public void WriteDataTo(BinaryWriter _Writer)
        {
            int numKeys = SavedQuestions.Keys.Count;
            _Writer.Write(numKeys);
            foreach (var key in SavedQuestions.Keys)
            {
                _Writer.Write(key);
                _Writer.WriteDictionary(SavedQuestions[key]);
            }
        }

        public void SetDataFrom(BinaryReader _Reader)
        {
            int numKeys = _Reader.ReadInt32();
            for (var i = 0; i < numKeys; i++)
            {
                int key = _Reader.ReadInt32();
                var value = _Reader.ReadDictionary<string, string>();
                SavedQuestions[key] = new Dictionary<string, string>(value);
            }
        }

        //NOTE: will not work where Entity's primary key is not an integer
        public object GetKey()
        {
            return Key;
        }

        public bool Ignore()
        {
            return false;
        }

        #endregion
    }
}