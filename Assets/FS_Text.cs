using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Assets.Components;

public class FS_Text : Conversation
{
    protected override IEnumerator DoConversation()
    {
        //Speaks two messages, split into two pages
        yield return Speak("It's a sword",
            "It has engraved text");

        //Presents a yes and no question
        yield return PresentYesNoQuestion("Read Text?");

        //Was the result of the question a yes?
        if (DialogBoxResult == YesNoResult.Yes)
        {
            yield return Speak("This sword dreams of YouTube");
        }
        //Was the result of the question a no?
        else if (DialogBoxResult == YesNoResult.No)
        {
            yield return Speak("too bad, click yes lol");
        }
    }
}
