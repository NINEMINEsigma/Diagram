using AD.UI;
using Diagram;

namespace Game
{
    public class MinnesStatsSharedPanel : LineBehaviour,IOnDependencyCompleting
    {
        public void OnDependencyCompleting()
        {
            //LineScript.RunScript("Stats.ls", ("this", this));
            StaticEditType = this.EditType;
            StaticNoteType = this.NoteType;
            SetStats();
        }

        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand), typeof(MinnesTimeline));
        }

        public void SetStats()
        {
            MinnesTimeline.instance.Stats.SetTitle($"{Minnes.ProjectName}-{NoteTypeName}-{EditTypeName}");
        }
        public void SetNoteTypeDefault(bool on)
        {
            if (on)
                NoteTypeName = "Note";
            SetStats();
        }
        public void SetEditTypeCreate(bool on)
        {
            if (on)
                EditTypeName = "Create";
            SetStats();
        }
        public void SetEditTypeDelete(bool on)
        {
            if (on)
                EditTypeName = "Delete";
            SetStats();
        }

        public ModernUIDropdown NoteType;
        public ModernUIDropdown EditType;

        public static ModernUIDropdown StaticNoteType;
        public static ModernUIDropdown StaticEditType;

        public static string NoteTypeName = "Note";
        public static string EditTypeName = "Create";
    }
}
