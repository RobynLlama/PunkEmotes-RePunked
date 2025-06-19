using System.Text.RegularExpressions;
using PunkEmotes.Components;

namespace PunkEmotes.Utilities;

public static partial class Utils
{
  public static void SanitizeChatString(ref string message)
  {

    //sanitizes the input, bad Punk!
    message = message.Replace(PunkEmotesManager.PUNK_NETWORK_SIGNATURE_DIRTY, PunkEmotesManager.PUNK_NETWORK_SIGNATURE_CLEAN);

    //remove all XML formatting of any type
    message = Regex.Replace(message, @"<.*?>", string.Empty);
  }
}
