using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Assets.Components;

public class FS_Lore_Text : Conversation
{
    protected override IEnumerator DoConversation()
    {
        //Speaks two messages, split into two pages
        yield return Speak("It's a Lore Tablet");

        //Presents a yes and no question
        yield return PresentYesNoQuestion("Read Text?");

        //Was the result of the question a yes?
        if (DialogBoxResult == YesNoResult.Yes)
        {
            yield return Speak("In the Regional Slugma lake within the opposite-colored path","Next to the scream coming from the abyss",
                "Hatch from the egg of infection","Near the Tyrant of [[Heart-Shaped Object]]");
        }
        //Was the result of the question a no?
        else if (DialogBoxResult == YesNoResult.No)
        {
            yield return Speak("too bad, click yes lol");
        }
    }
}
