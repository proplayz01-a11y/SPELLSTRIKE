using System;

namespace Spellstrike.ComicMode
{
    /// <summary>
    /// Caption pass for the 42-panel 3D prologue. Locked text comes verbatim
    /// from docs/STORY_CANVA_DIALOGUE_SCRIPT.md. The remaining proposed lines
    /// stay subject to narrative review; intentionally silent reaction shots
    /// remain silent. This does not rewrite the historical Comic Mode art.
    /// </summary>
    public static class CinematicPrologueDialogueV06
    {
        public readonly struct Cue
        {
            public readonly string Speaker;
            public readonly string Text;
            public readonly string SecondSpeaker;
            public readonly string SecondText;
            public readonly double SwitchAtSeconds;

            public Cue(string speaker, string text,
                string secondSpeaker = null, string secondText = null,
                double switchAtSeconds = double.PositiveInfinity)
            {
                Speaker = speaker;
                Text = text;
                SecondSpeaker = secondSpeaker;
                SecondText = secondText;
                SwitchAtSeconds = switchAtSeconds;
            }
        }

        private static readonly Cue[] Lines =
        {
            new Cue("Narrator", "Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali..."), // P01 LOCKED
            new Cue("Narrator", "...until Broken Language spread across the kingdoms, twisting Words into weapons of ruin."), // P02 LOCKED
            new Cue("Narrator", "Van fought back with every Word he knew - but even his strongest magic could not stop the war."), // P03 LOCKED
            new Cue("Narrator", "When the war finally ended, Van returned home - only to find his wife gone and his daughter, Nevali, nowhere to be found."), // P04 LOCKED
            new Cue("Narrator", "With nowhere else left to turn, Van sought refuge in the library of his former master, Vander Poel."), // P05 LOCKED
            new Cue("Van Aert", "The war took my wife."), // P06 LOCKED
            new Cue("Van Aert", "And my daughter, Nevali... I searched everywhere, but I could not find her."), // P07 LOCKED
            new Cue(null, null), // P08 silent: Vander processes the confession
            new Cue("Narrator", "Decades passed..."), // P09 proposed time card
            new Cue("Van Aert", "I dreamed of Nevali again. She was calling to me... as though she were somewhere close."), // P10 dream confession
            new Cue("Nevali", "Father... can you hear me?", "Van Aert", "Nevali...?", 4.0), // P11 dream and response
            new Cue(null, null), // P12 silent: Vander reflects
            new Cue(null, null), // P13 silent: Vander contemplates
            new Cue("Vander Poel", "That was no ordinary dream. Something within this library is reaching out to you."), // P14 proposed
            new Cue("Vander Poel", "Come with me. There is something you must see."), // P15 proposed
            new Cue(null, null), // P16 silent: library transition
            new Cue("Narrator", "From the deepest shelf, Vander retrieved a volume untouched for generations."), // P17 proposed shared narration
            new Cue("Narrator", "From the deepest shelf, Vander retrieved a volume untouched for generations."), // P18 narration continues
            new Cue("Vander Poel", "This book records the Great War... and the force that corrupted the power of Words."), // P19 proposed
            new Cue("Vander Poel", "It consumed every kingdom it touched."), // P20 proposed
            new Cue("Vander Poel", "In the end, the black-robed entity was defeated and sealed away."), // P21 proposed
            new Cue(null, null), // P22 silent: Van's suspicion
            new Cue("Van Aert", "You speak as though you knew this entity. How do you know so much about it?"), // P23 proposed
            new Cue("Vander Poel", "Our paths crossed during the war. I witnessed its fall. Nothing more."), // P24 proposed
            new Cue(null, null), // P25 silent: book illustrations dissolve
            new Cue(null, null), // P26 LOCKED Nevali's complete message remains WRITTEN inside the book
            new Cue(null, null), // P27 silent: Van recognizes his daughter's handwriting
            new Cue("Van Aert", "Why is my daughter's name inside this book?"), // P28 proposed
            new Cue("Vander Poel", "Van... this is no ordinary volume."), // P29 LOCKED
            new Cue("Vander Poel", "It is a living prison - an entire world sealed within its pages. If Nevali wrote this message... then she is trapped inside."), // P30 LOCKED
            new Cue("Van Aert", "Then tell me how to get inside."), // P31 LOCKED
            new Cue("Vander Poel", "There is a way. But it requires a forbidden technique - one no wizard is permitted to use."), // P32 LOCKED
            new Cue("Van Aert", "Forbidden or not, I will not leave my daughter in there."), // P33 LOCKED
            new Cue("Vander Poel", "Wait here. I must retrieve my staff."), // P34 LOCKED
            new Cue(null, null), // P35 silent: Vander returns with staff
            new Cue("Vander Poel", "Prepare yourself. Once the spell begins, I cannot stop it."), // P36 LOCKED
            new Cue(null, null), // P37 silent: Van is pulled into the book
            new Cue("Van Aert", "Where... am I?"), // P38 proposed
            new Cue(null, null), // P39 silent sunlit first-person awakening
            new Cue(null, null), // P40 silent physical action
            new Cue(null, null), // P41 silent Van retrieves his staff
            new Cue("Narrator", "With no path back, Van followed the road ahead.") // P42 proposed closing narration
        };

        public static int PanelCount => Lines.Length;

        public static Cue GetCue(int zeroBasedPanel, double secondsIntoPanel)
        {
            if (zeroBasedPanel < 0 || zeroBasedPanel >= Lines.Length)
                return default;

            Cue cue = Lines[zeroBasedPanel];
            if (cue.SecondText != null && secondsIntoPanel >= cue.SwitchAtSeconds)
                return new Cue(cue.SecondSpeaker, cue.SecondText);
            return cue;
        }
    }
}
