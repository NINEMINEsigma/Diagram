//Generate a long note with long body
new(note1) Game.NoteGenerater()
note1 -> target -> InitPosition(@StartX,@StartY,@StartZ)
note1 -> target -> MakeMovement(@StartTime,@JudgeTime,@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note1 -> target -> MakeMovement(@JudgeTime,@SongEndTime,@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note1 -> target -> MakeScale(@StartTime,@JudgeTime,1,1,1,1,1,1,0)
note1 -> target -> MakeScale(@JudgeTime,@SongEndTime,0,0,0,0,0,0,0)
note1 -> target -> InitNote(@JudgeTime,@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note1 -> target -> RegisterOnTimeLine()
new(note2) Game.NoteGenerater()
note2 -> target -> InitPosition(@StartX,@StartY,@StartZ)
note2 -> target -> MakeMovement("@HoldTime+@StartTime","@HoldTime+@JudgeTime",@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note2 -> target -> MakeMovement("@HoldTime+@JudgeTime","@HoldTime+@SongEndTime",@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note2 -> target -> MakeScale("@HoldTime+@StartTime","@HoldTime+@JudgeTime",1,1,1,1,1,1,0)
note2 -> target -> MakeScale("@HoldTime+@JudgeTime",@SongEndTime,0,0,0,0,0,0,0)
note2 -> target -> InitNote("@HoldTime+@JudgeTime",@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note2 -> target -> RegisterOnTimeLine()
new(long) Game.LongNoteBodyGenerater()
long -> target -> AddNoteGenerater(note1)
long -> target -> AddNoteGenerater(note2)
long -> target -> MakeRebuildInterval(@StartTime,"@HoldTime+@JudgeTime")
long -> target -> RegisterOnTimeLine()