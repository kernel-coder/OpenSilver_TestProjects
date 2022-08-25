#region Usings

using System;

#endregion

namespace Virtuoso.Core.Model
{
    public class HeightWeightCalculations
    {
        protected const float MaxHeightCm = 260f; // 8'6"
        protected const float MaxWeightKg = 650f; // 1433lbs

        public static float CvtWeightKg(string scale, float value)
        {
            if (scale != null && scale.ToLower() == "lb")
            {
                return value * .45359237F;
            }

            return value;
        }

        public static float CvtHeightCm(string scale, float value)
        {
            string hs = (scale != null) ? scale.ToLower() : "cm"; // Default to cm

            if (hs.Equals("in"))
            {
                return value * 2.54F;
            }

            if (hs.Equals("m"))
            {
                return value * 100;
            }

            return value;
        }

        public static float? CalculateBMIValue(float cm, float kg)
        {
            float? resultparsed = null;

            if (cm > 0 && cm < MaxHeightCm && kg > 0 && kg < MaxWeightKg)
            {
                float m = cm / 100;
                float result = (kg / (m * m)) + 0.05F;
                resultparsed = float.Parse(string.Format("{0:0.0}", result));
            }

            return resultparsed;
        }

        public static float? CalculateBSAValue(float cm, float kg, string formulaName)
        {
            if (formulaName != null && cm > 0 && cm < MaxHeightCm && kg > 0 && kg < MaxWeightKg)
            {
                string formula = formulaName.Trim().ToLower();
                double bsa = 0.0;
                if (formula == "" || formula == "mosteller")
                {
                    //BSA (m²) = ( [Height(cm) x Weight(kg) ]/ 3600 )^(.5)        e.g. BSA = SQRT( (cm*kg)/3600 )
                    bsa = Math.Sqrt((cm * kg) / 3600);
                }
                else if (formula == "dubois")
                {
                    //BSA (m²) = 0.20247 x Height(m)0.725 x Weight(kg)0.425
                    bsa = 0.20247 * Math.Pow((cm / 100), 0.725) * Math.Pow(kg, 0.425);
                }
                else if (formula == "haycock")
                {
                    //BSA (m²) = 0.024265 x Height(cm)0.3964 x Weight(kg)0.5378
                    bsa = 0.024265 * Math.Pow(cm, 0.3964) * Math.Pow(kg, 0.5378);
                }
                else if (formula == "gehan")
                {
                    //BSA (m²) = 0.0235 x Height(cm)0.42246 x Weight(kg)0.51456
                    bsa = 0.0235 * Math.Pow(cm, 0.42246) * Math.Pow(kg, 0.51456);
                }
                else if (formula == "boyd")
                {
                    //BSA (m2) = 0.0003207 x Height(cm)0.3 x Weight(grams)(0.7285 - ( 0.0188 x LOG(grams) )
                    bsa = 0.0003207 * Math.Pow(cm, 0.3) *
                          Math.Pow(kg * 1000, (0.7285 - (0.0188 * Math.Log(kg * 1000))));
                }

                var resultstr = string.Format("{0:0.0}", (float)bsa);
                return float.Parse(resultstr);
            }

            return null;
        }
    }
}