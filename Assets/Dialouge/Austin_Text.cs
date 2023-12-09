using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Assets.Components;

public class Austin_Text : Conversation
{
    protected override IEnumerator DoConversation()
    {
        throw new System.NotImplementedException();

        //Dialouge
        yield return Speak("If you watch him and know about opposite colors, the place should be obvious");
    }
}
