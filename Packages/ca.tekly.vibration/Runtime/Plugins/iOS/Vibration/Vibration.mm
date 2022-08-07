#import <AudioToolbox/AudioToolbox.h>

extern "C" {
    bool _HasVibration () {
        return !(UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
    }
 
    void _Vibrate () {
        AudioServicesPlaySystemSoundWithCompletion(1352, NULL);
    }
    
    void _VibratePeak () {
        AudioServicesPlaySystemSoundWithCompletion(1519, NULL);
    }

    void _VibratePop () {
        AudioServicesPlaySystemSoundWithCompletion(1520, NULL);
    }

    void _VibrateNegative () {
        AudioServicesPlaySystemSoundWithCompletion(1521, NULL);
    }
}
