namespace OWSBG.Core
{
    /// <summary>
    /// Windreach's regional decision (bible 4.5): whether the Steppe is ever surveyed. Surveying it would let the Guild
    /// anchor it. Two birds can finish that survey: Surveyor Hale, if Wren lets him (6.9), and Wren herself, at
    /// Idrenne's Fire. Or she walks it with the clans instead, which holds it their way (the true ending's way, 9.2).
    /// The endings (DES-12) read this.
    /// </summary>
    public static class Steppe
    {
        public const string HaleFinishedFlag = "windreach.hale.finished";
        /// <summary>1 drawn by Wren, 2 left undrawn, 3 walked with the clans; 0 undecided.</summary>
        public const string SurveyFlag = "windreach.survey.decided";
        public const string FireWitnessedFlag = "windreach.fire.witnessed";
        public const string KeystoneFlag = "keystone.windreach";
        public const int Drawn = 1, Undrawn = 2, Walked = 3;

        /// <summary>The Steppe is on paper the Guild can anchor from: Hale finished, or Wren drew it.</summary>
        public static bool OnTheGuildsMap(WorldState w) => w.Is(HaleFinishedFlag) || w.Get(SurveyFlag) == Drawn;

        /// <summary>Wren walks it with the clans: held by being lived in, not by a stone.</summary>
        public static bool HeldByWalking(WorldState w) => w.Get(SurveyFlag) == Walked;

        /// <summary>Bible 9.2 asks for this by name: the game said the true ending's requirement aloud, and she heard it.</summary>
        public static bool FireWitnessed(WorldState w) => w.Is(FireWitnessedFlag);
    }
}
