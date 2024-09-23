namespace Diagram
{
    public class BuffManager : BaseWrapper.BuffFrames
    {
        [_Init_]
        public BuffManager() : base(null) { }

        public object target;
        public void ExecuteBuff()
        {
            this.Diffusing("Execute", this);
        }
    }

    public class BuffWrapper : BaseWrapper.Model
    {
        public BuffWrapper(Buff buff) : base(buff) { }
    }

    public class Buff
    {
        public string BuffScript;
        public string Buffer;

        public virtual void Execute(object element)
        {
            LineScript.RunScript(BuffScript, ("this", element), ("buffer", this.Buffer));
        }
    }
}
