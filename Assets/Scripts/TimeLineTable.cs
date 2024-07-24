using System.Collections;
using System.Collections.Generic;
using AD.UI;
using Diagram;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    public class TimeLineTable : MinnesController,IOnDependencyCompleting,IPointerClickHandler
    {
        public void OnDependencyCompleting()
        {
            LineScript.RunScript("TimeTable.ls", ("this", this));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var rects = this.transform.As<RectTransform>().GetRect();
            float x = (eventData.position.x - rects[0].x) / (rects[2].x - rects[0].x);
            float y = (eventData.position.y - rects[0].y) / (rects[1].y - rects[0].y);
            LineScript.RunScript("TableClick.ls",
                ("this", this),
                ("@x", x),
                ("@y", y),
                ("time",Minnes.MinnesInstance.CurrentTick),
                ("length",this.Architecture<Minnes>().GetController<MinnesTimeline>().To<MinnesTimeline>().TimeLineDisplayLength));
        }

        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand),typeof(MinnesTimeline));
        }


    }
}
