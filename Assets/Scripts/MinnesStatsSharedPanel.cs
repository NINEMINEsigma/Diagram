using AD.UI;
using Diagram;

namespace Game
{
    public class MinnesStatsSharedPanel : LineBehaviour,IOnDependencyCompleting
    {
        public void OnDependencyCompleting()
        {
            LineScript.RunScript("Stats.ls", ("this", this));
            StaticEditType = this.EditType;
            StaticNoteType = this.NoteType;
        }

        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand), typeof(MinnesTimeline));
        }

        public void SetStats()
        {
            MinnesTimeline.instance.Stats.buttonText = $"{NoteTypeName}-{EditTypeName}";
        }
        public void SetNoteTypeDefault(bool on)
        {
            if (on)
                NoteTypeName = "Default";
            SetStats();
        }
        public void SetEditTypeCreate(bool on)
        {
            EditTypeName = on ? "Create" : "Delete";
            SetStats();
        }

        public ModernUIDropdown NoteType;
        public ModernUIDropdown EditType;

        public static ModernUIDropdown StaticNoteType;
        public static ModernUIDropdown StaticEditType;

        public static string NoteTypeName = "Create";
        public static string EditTypeName = "Default";
    }
}
