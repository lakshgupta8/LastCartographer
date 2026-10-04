using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The face a talker lends whoever speaks through it (<see cref="OWSBG.Core.Portraits.InTheirBody"/>, portraits.md
    /// §1): an island's unnamed person speaks as <c>Remnant</c> in the look the island stood them in, so the page shows
    /// that look's portrait. Set by whoever dresses the talker (IslandBuilder), with the sheet loaded alongside its
    /// drawings and let go with the room.
    /// </summary>
    public sealed class PortraitFace : MonoBehaviour
    {
        /// <summary>The body the face is drawn from: a townsfolk look's character, "Folk_Gull".</summary>
        public string Body;
        public Texture2D Sheet;

        public static PortraitFace Lend(GameObject talker, string body, Texture2D sheet)
        {
            if (talker == null || sheet == null) return null;
            var face = talker.GetComponent<PortraitFace>() ?? talker.AddComponent<PortraitFace>();
            face.Body = body;
            face.Sheet = sheet;
            return face;
        }
    }
}
