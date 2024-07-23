//Generate a note-base
new(note) Game.NoteGenerater()
note -> target -> InitPosition(@StartX,@StartY,@StartZ)
note -> target -> MakeMovement(@StartTime,@JudgeTime,@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note -> target -> MakeMovement(@JudgeTime,@SongEndTime,@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note -> target -> MakeScale(@StartTime,@JudgeTime,1,1,1,1,1,1,0)
note -> target -> MakeScale(@JudgeTime,@SongEndTime,1,1,1,1,1,1,0)
note -> target -> InitNote(@JudgeTime,@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note -> target -> RegisterOnTimeLine()