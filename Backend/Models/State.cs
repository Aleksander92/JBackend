using Microsoft.Build.Framework;

namespace Backend.Models;

public class ModeState {
    public enum EMode {
        EModeHiraganaToEnglish,
        EModeEnglishToHiragana,
        EModeKatakanaToEnglish,
        EModeEnglishToKatakana
    };

    public int Id { get; set; }
    public EMode Mode { get; set; }
    public int Attempts { get; set; } = 0;
    public int CorrectAttempts { get; set; } = 0;
    public string? TaskQuestion { get; set; }
    public string? TaskAnswer { get; set; }

    //////////////////////////////////////////////////////////////////////////////////////////////

    public ModeState() {}

    public ModeState(int id, int mode, (string, string) task) {
        Id = id;
        Mode = (EMode)mode;
        TaskQuestion = task.Item1;
        TaskAnswer = task.Item2;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    
    public void SetTask((string, string) task) { 
        TaskQuestion = task.Item1;
        TaskAnswer = task.Item2;
    }
}