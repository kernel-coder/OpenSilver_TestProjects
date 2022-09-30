using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Virtuoso.Core.View;
using Virtuoso.Server.Data;
using Annotation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Virtuoso.Core.Controls.AniMan;
using GalaSoft.MvvmLight.Messaging;
using static Virtuoso.Core.Controls.AniMan.AniManForWoundLocation;
using System.Windows.Media;
using System.Windows.Resources;

namespace Virtuoso.Core.Controls
{
    public partial class WoundPopupV2 : UserControl
    {
        private AniManForWoundLocation animan;

        public int? LocallyTrackedPartId = null;

        public static Image LoadDisplayPNG(System.Windows.Media.Imaging.BitmapImage bi)
        {
            var img = new Image();

            bi.ImageOpened += (s, e) => Debug.WriteLine(e.ToString());
            bi.ImageFailed += (s, e) => Debug.WriteLine(e.ToString());

            ImageSource imgSrc = bi;
            img.SetValue(Image.SourceProperty, imgSrc);
            img.SetValue(Image.StretchProperty, Stretch.None);
            img.SetValue(Image.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
            img.SetValue(Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Top);
            return img;
        }

        public static Image LoadDisplayPNG(StreamResourceInfo resourceInfo)
        {
            var bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.SetSource(resourceInfo.Stream);

            return LoadDisplayPNG(bi);
        }

        public static Image LoadDisplayPNG(string filename)
        {
            Uri uri = new Uri(filename, UriKind.Relative);
            var bi = new System.Windows.Media.Imaging.BitmapImage(uri);
            bi.UriSource = uri;
            return LoadDisplayPNG(bi);
        }

        public WoundPopupV2()
        {
            InitializeComponent();
            Image Image1 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/Body.png");
            Image Image2 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/LeftFootLateral.png");
            Image Image3 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/RightFootLateral.png");
            Image Image4 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/RightFootMedial.png");
            Image Image5 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/LeftFootMedial.png");
            Image Image6 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/LeftFootDorsal.png");
            Image Image7 = LoadDisplayPNG("/ALY_Button_Style;component/Assets/Images/RightFootDorsal.png");

            animan = new AniManForWoundLocation(Image1, Image2, Image3, Image4, Image5, Image6, Image7);
            animan.AniManCtrl1.DrawSelectionMode(null, this.AniManCanvas1);

            animan.DisplayDetailBasedOnSelectedPart(Colors.White, this.AniManCanvas2, this.AniManCanvas3,
                this.AniManCanvas4, this.AniManCanvas5, this.AniManCanvas6, this.AniManCanvas7);

            this.OptionSource = new ObservableCollection<ListItem>();
            this.Options.DataContext = this.OptionSource;

            var token = "SomethingUniqueEventually?";
            Messenger.Default.Register<int>(this, token, AniMan_Dialog_Show);
        }

        public static readonly DependencyProperty IsLockedProperty =
            DependencyProperty.Register("IsLocked", typeof(bool),
                typeof(WoundPopupV2), new PropertyMetadata(new PropertyChangedCallback(IsLockedChanged)));

        public static readonly DependencyProperty SlaveProperty =
            DependencyProperty.Register("Slave", typeof(int),
                typeof(WoundPopupV2), new PropertyMetadata(new PropertyChangedCallback(SlaveChanged)));

        public bool IsLocked
        {
            get { return (bool)GetValue(IsLockedProperty); }
            set { SetValue(IsLockedProperty, value); }
        }

        private static void IsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
            }
        }

        public AniManUIStruct UsingGetUIStruct()
        {
            return new AniManUIStruct(this.animan, this.AniManCanvas1, this.AniManCanvas2, this.AniManCanvas3,
                this.AniManCanvas4, this.AniManCanvas5, this.AniManCanvas6, this.AniManCanvas7);
        }

        public int Slave
        {
            get { return (int)GetValue(SlaveProperty); }
            set { SetValue(SlaveProperty, value); }
        }

        private static void SlaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var ctrl = (WoundPopupV2)d;

                var viewModel = (IAniManControlDataContext)ctrl.DataContext;
                using (var s = ctrl.UsingGetUIStruct())
                {
                    if (viewModel.AniManReset.CanExecute(s))
                    {
                        viewModel.AniManReset.Execute(s);
                    }
                }
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
            }
        }

        public void AniMan_Dialog_Show(int admissionWoundSiteKey)
        {
            this.OptionSource.Clear();
            LocallyTrackedPartId = null;
        }

        private void Options_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var viewModel = (IAniManControlDataContext)DataContext;
            using (var s = new AniManUIStruct(this.animan, this.AniManCanvas1, this.AniManCanvas2, this.AniManCanvas3,
                       this.AniManCanvas4, this.AniManCanvas5, this.AniManCanvas6, this.AniManCanvas7))
            {
                Annotation.ListItem item = (Annotation.ListItem)this.Options.SelectedItem;
                if (item != null)
                {
                    Debug.WriteLine(item.Id);
                    Debug.WriteLine(item.Description);

                    s.AniMan.SelectedPart = item.Id;

                    this.LocallyTrackedPartId = item.Id;

                    this.OptionSource.Clear();

                    if (viewModel.AniManSelectPartOnOptionsMenu.CanExecute(s))
                    {
                        viewModel.AniManSelectPartOnOptionsMenu.Execute(s);
                    }
                }
                else
                {
                    s.AniMan.SelectedPart = null;
                }
            }
        }

        public ObservableCollection<ListItem> OptionSource { get; set; }

        public void ToggleAniMan_Click(object sender, RoutedEventArgs eventArgs)
        {
            this.OptionSource.Clear();
            var viewModel = (IAniManControlDataContext)DataContext;
            using (var s = new AniManUIStruct(this.animan, this.AniManCanvas1, this.AniManCanvas2, this.AniManCanvas3,
                       this.AniManCanvas4, this.AniManCanvas5, this.AniManCanvas6, this.AniManCanvas7))
            {
                if (viewModel.AniManToggleDisplay.CanExecute(s))
                {
                    viewModel.AniManToggleDisplay.Execute(s);
                }
            }
        }

        public void AnimanMouseOver(object sender, MouseEventArgs eventArgs, Canvas canvas, int index)
        {
            var point = new Coordinate(eventArgs.GetPosition(canvas));
            var part = animan.FindPartUnderPoint(point);
            if (part.HasValue)
            {
                var polygonID = animan.FindPolygonUnderPoint(point);
                this.Selected.Text = animan.PartDescription(part.Value, polygonID);
            }
            else
            {
                this.Selected.Text = string.Empty;
            }
        }

        public void AnimanMouseOver1(object sender, MouseEventArgs eventArgs)
        {
            AnimanMouseOver(sender, eventArgs, this.AniManCanvas1, 1);
        }

        public void AnimanMouseLeave(object sender, MouseEventArgs eventArgs)
        {
            this.Selected.Text = string.Empty;
        }

        public void AnimanLeftClick(object sender, MouseButtonEventArgs eventArgs)
        {
            if (this.IsLocked)
            {
                Debug.WriteLine("Locked");
                return;
            }

            this.Selected.Text = string.Empty;

            var viewModel = (IAniManControlDataContext)DataContext;

            var point = new Coordinate(eventArgs.GetPosition(this.AniManCanvas1));

            var selectedPart = animan.FindPartUnderPoint(point);

            if (!selectedPart.HasValue || (selectedPart.HasValue && selectedPart.Value == 0))
            {
                selectedPart = 10000;
            }

            using (var s = new AniManUIStruct(this.animan, this.AniManCanvas1, this.AniManCanvas2, this.AniManCanvas3,
                       this.AniManCanvas4, this.AniManCanvas5, this.AniManCanvas6, this.AniManCanvas7))
            {
                var oldpart = s.AniMan.SelectedPart;
                s.AniMan.SelectedPart = selectedPart;
                if (viewModel.AniManSelectPartOnSilhouette.CanExecute(s))
                {
                    viewModel.AniManSelectPartOnSilhouette.Execute(s);
                }

                if (s.WasSuccess)
                {
                    animan.AnimanLeftClick(point, this.AniManCanvas1, true);
                    this.OptionSource.Clear();

                    if (selectedPart.HasValue)
                    {
                        var h = animan.AniManCtrl1.GetPartHeirarchy(selectedPart.Value);
                        if (h.Any() == true)
                        {
                            ListItem topmost = null;
                            foreach (var item in h)
                            {
                                if (topmost == null)
                                {
                                    topmost = item;
                                }

                                this.OptionSource.Add(item);
                            }

                            this.Options.ScrollIntoView(topmost);
                        }
                    }

                    LocallyTrackedPartId = selectedPart;
                }
                else
                {
                    s.AniMan.SelectedPart = oldpart;
                }
            }

            eventArgs.Handled = true;
        }

        public void AnimanRightClick(object sender, MouseButtonEventArgs eventArgs)
        {
            this.animan.ToSQL();

            eventArgs.Handled = true;
        }

        private void OasisStatusHelpButton_Click(object sender, RoutedEventArgs e)
        {
            //AdmissionWoundSite wound = null;
            //Button b = sender as Button;
            //if (b != null) wound = b.Tag as AdmissionWoundSite;
            //OasisStatusHelpChildWindow cw = new OasisStatusHelpChildWindow(wound);
            //cw.Show();
        }

#region "Popup Help Template Logic"

        private void PressureUlcerHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(PressureUlcerContent(), "Pressure Ulcer Stages");
        }

        private void DrainageAmountHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(DrainageContent(), "Drainage Amounts");
        }

        private void WoundTypeHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(SurgicalWoundContent(), "Surgical Wound Guidance");
        }

        private void BurnDegreeHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(BurnDegreeContent(), "Degree of Burns");
        }

        private void ShowHelpDialog(Paragraph templateContent, string title)
        {
            var helpDialog = new HelpPopupDialog(templateContent, title);
            helpDialog.Show();
        }

        private Paragraph SurgicalWoundContent()
        {
            var templateParagraph = new Paragraph();
            var templateContent = templateParagraph.Inlines;

            templateContent.Add(RichTextHelper.BoldLine("Include"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Wounds resulting from a surgical procedure that have not become a scar"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLineSub(
                "surgical wound closed  primarily(sutures, staples or a chemical bonding agent) is described as a surgical wound until re epithelialization has been present for approximately 30 days.  After 30 days it is described as a scar and should not be included"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLineSub("Observable: can be seen/visualized"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLineSub(
                "Unobservable: cannot be visualized due to a cast or dressing that cannot be removed due to a physician's order."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine(
                "Implanted venous access devices even if the implantation site has healed. Device does not need to be functional or accessed. These are central lines placed by a surgical procedure."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Orthopedic pin sites, central line sites, stapled or sutured incisions."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Peritoneal dialysis catheter, AV shunt."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine(
                "Wounds with drains, even after the drain is pulled until it heals and becomes a scar."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine(
                "Surgical incision with well approximated edges and a scab (i.e., crust) from dried blood or tissue fluid."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine(
                "Muscle flap, skin advancement flap or rotational flap to surgically replace a pressure ulcer."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Gastrostomy closed by a surgical 'take down' procedure."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine(
                    "A shave, punch or excisional biopsy to remove and/or diagnose skin lesions."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Abscess treated by incision and drainage with placement of a drain."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Surgical repair of a traumatic injury."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine(
                    "Arthrocentesis site when a surgical procedure is performed by arthroscopy."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BoldLine("Exclude"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BulletLine("A surgical scar"));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLineSub(
                "Surgical wound that has been completely epithelialized for about 30 days or more with no S/S of infection and no evidence of complications."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine(
                    "All 'ostomies' except a bowel ostomy closed by a surgical 'take down' procedure."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Debridement or the placement of skin grafts."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("PICC lines (peripherally inserted)."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Gastrostomy allowed to close on its own without surgical intervention."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Pressure ulcers treated by surgical debridement."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Suturing of a traumatic laceration."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Abscess treated by incision and drainage without placement of a drain."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine(
                "Cataract surgery, surgery to the mucosal membranes or a gynecological surgical procedure via a vaginal approach (wound is not of the integument)."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(
                RichTextHelper.BulletLine("Aspiration of fluid by needle without placement of a drain."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.BulletLine("Cardiac catheterization performed by needle puncture."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.PlainLine(
                "WOCN reference Wound, Ostomy and Continence Nurses Society’s Guidance on OASIS-C1 Integumentary Items: Best Practice for Clinicians, Copyright© 2014 by the Wound, Ostomy and Continence Nurses Society™. Date of Publication: 2/28/2014 at "));
            templateContent.Add(RichTextHelper.Hyperlink("http://www.wocn.org", "http://www.wocn.org"));
            templateContent.Add(RichTextHelper.NewLine());
            return templateParagraph;
        }

        private Paragraph BurnDegreeContent()
        {
            var templateParagraph = new Paragraph();
            var templateContent = templateParagraph.Inlines;

            templateContent.Add(RichTextHelper.BoldLine("First Degree Burns"));
            templateContent.Add(RichTextHelper.PlainLine(
                "A superficial burn in which damage is limited to the outer layer of the epidermis and is marked by redness, tenderness and mild pain.  Blisters do not form and the burn heals without scar formation.  A common example is sunburn."));
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BoldLine("Second Degree Burns"));
            templateContent.Add(RichTextHelper.PlainLine(
                "A burn that damages partial thickness of the epidermal and some dermal tissue but does not damage the lower-lying hair follicles, sweat or sebaceous glands.  The burn is painful and red; blisters form and wounds may heal with a scar."));
            templateContent.Add(RichTextHelper.NewLine());


            templateContent.Add(RichTextHelper.BoldLine("Third Degree Burns"));
            templateContent.Add(RichTextHelper.PlainLine(
                "A burn that extends through the full thickness of the skin and subcutaneous tissues beneath the dermis.  The burn leaves skin with a pale, brown, gray or blackened appearance.   The burn is often painless because it destroys nerves in the skin.  Scar formation and contractures are likely complications."));
            templateContent.Add(RichTextHelper.NewLine());


            return templateParagraph;
        }

        private Paragraph DrainageContent()
        {
            var templateParagraph = new Paragraph();
            var templateContent = templateParagraph.Inlines;

            templateContent.Add(RichTextHelper.BoldLine("Drainage (or exudate) is considered"));
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BoldText("light"));
            templateContent.Add(RichTextHelper.PlainText(" when it covers less than 25% of the wound dressing"));
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BoldText("moderate"));
            templateContent.Add(RichTextHelper.PlainText(" when it covers 50 to 75% of the wound dressing"));
            templateContent.Add(RichTextHelper.NewLine());


            templateContent.Add(RichTextHelper.BoldText("bold"));
            templateContent.Add(RichTextHelper.PlainText(" when it covers 75% to 100% of the wound dressing"));
            templateContent.Add(RichTextHelper.NewLine());

            return templateParagraph;
        }

        private Paragraph PressureUlcerContent()
        {
            var templateParagraph = new Paragraph();
            var templateContent = templateParagraph.Inlines;

            templateContent.Add(
                RichTextHelper.BoldLine("Stage 1 Pressure Injury: Non-blanchable erythema of intact skin"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Intact skin with a localized area of non-blanchable erythema, which may appear differently in darkly pigmented skin. Presence of blanchable erythema or changes in sensation, temperature, or firmness may precede visual changes. Color changes do not include purple or maroon discoloration; these may indicate deep tissue pressure injury."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(
                RichTextHelper.BoldLine("Stage 2 Pressure Injury: Partial-thickness skin loss with exposed dermis"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Partial-thickness loss of skin with exposed dermis. The wound bed is viable, pink or red, moist, and may also present as an intact or ruptured serum-filled blister. Adipose (fat) is not visible and deeper tissues are not visible. Granulation tissue, slough and eschar are not present. These injuries commonly result from adverse microclimate and shear in the skin over the pelvis and shear in the heel.  This stage should not be used to describe moisture associated skin damage (MASD) including incontinence associated dermatitis (IAD), intertriginous dermatitis (ITD), medical adhesive related skin injury (MARSI), or traumatic wounds (skin tears, burns, abrasions)."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BoldLine("Stage 3 Pressure Injury: Full-thickness skin loss"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Full-thickness loss of skin, in which adipose (fat) is visible in the ulcer and granulation tissue and epibole (rolled wound edges) are often present. Slough and/or eschar may be visible. The depth of tissue damage varies by anatomical location; areas of significant adiposity can develop deep wounds.  Undermining and tunneling may occur. Fascia, muscle, tendon, ligament, cartilage and/or bone are not exposed. If slough or eschar obscures the extent of tissue loss this is an Unstageable Pressure Injury."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(
                RichTextHelper.BoldLine("Stage 4 Pressure Injury: Full-thickness skin and tissue loss"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Full-thickness skin and tissue loss with exposed or directly palpable fascia, muscle, tendon, ligament, cartilage or bone in the ulcer. Slough and/or eschar may be visible. Epibole (rolled edges), undermining and/or tunneling often occur. Depth varies by anatomical location. If slough or eschar obscures the extent of tissue loss this is an Unstageable Pressure Injury."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(
                RichTextHelper.BoldLine("Unstageable Pressure Injury: Obscured full-thickness skin and tissue loss"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Full-thickness skin and tissue loss in which the extent of tissue damage within the ulcer cannot be confirmed because it is obscured by slough or eschar.  If slough or eschar is removed, a Stage 3 or Stage 4 pressure injury will be revealed. Stable eschar (i.e. dry, adherent, intact without erythema or fluctuance) on the heel or ischemic limb should not be softened or removed."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.BoldLine(
                "Deep Tissue Pressure Injury: Persistent non-blanchable deep red, maroon or purple discoloration"));
            templateContent.Add(RichTextHelper.PlainLine(
                "Intact or non-intact skin with localized area of persistent non-blanchable deep red, maroon, purple discoloration or epidermal separation revealing a dark wound bed or blood filled blister. Pain and temperature change often precede skin color changes. Discoloration may appear differently in darkly pigmented skin.  This injury results from intense and/or prolonged pressure and shear forces at the bone-muscle interface.  The wound may evolve rapidly to reveal the actual extent of tissue injury, or may resolve without tissue loss. If necrotic tissue, subcutaneous tissue, granulation tissue, fascia, muscle or other underlying structures are visible, this indicates a full thickness pressure injury (Unstageable, Stage 3 or Stage 4). Do not use DTPI to describe vascular, traumatic, neuropathic, or dermatologic conditions."));
            templateContent.Add(RichTextHelper.NewLine());
            templateContent.Add(RichTextHelper.NewLine());

            templateContent.Add(RichTextHelper.PlainLine("Source: "));
            templateContent.Add(RichTextHelper.Hyperlink(
                "http://www.npuap.org/resources/educational-and-clinical-resources/npuap-pressure-injury-stages",
                "http://www.npuap.org/resources/educational-and-clinical-resources/npuap-pressure-injury-stages"));
            templateContent.Add(RichTextHelper.NewLine());

            return templateParagraph;
        }

#endregion

        //Surgical Wound Guidance
        private void PushScoreHelpButton_Click(object sender, RoutedEventArgs e)
        {
            //AdmissionWoundSite wound = null;
            //Button b = sender as Button;
            //if (b != null) wound = b.Tag as AdmissionWoundSite;
            //PushScoreHelpChildWindow cw = new PushScoreHelpChildWindow(wound);
            //cw.Show();
        }

        private void PushScoreGraphButton_Click(object sender, RoutedEventArgs e)
        {
            //AdmissionWoundSite wound = null;
            //Button b = sender as Button;
            //if (b != null) wound = b.Tag as AdmissionWoundSite;
            //if (wound == null) return;
            //DynamicFormGraph dfg = new DynamicFormGraph(wound.Admission, null, "Wound PUSH© scores", wound);
            //dfg.Show();
        }

        private void woundGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummaryItem valsumremove =
                valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

            if (e.Action == ValidationErrorEventAction.Removed)
            {
                valSum.Errors.Remove(valsumremove);
            }
            else if (e.Action == ValidationErrorEventAction.Added)
            {
                if (valsumremove == null)
                {
                    ValidationSummaryItem vsi = new ValidationSummaryItem()
                        { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
                    vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

                    valSum.Errors.Add(vsi);

                    e.Handled = true;
                }
            }
        }
    }
}