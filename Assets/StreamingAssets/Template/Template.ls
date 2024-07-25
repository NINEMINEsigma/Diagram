call "Note1.ls" import "@StartTime = 0" "@JudgeTime = 5"
call "Note2.ls" import "@StartTime = 5" "@JudgeTime = 10"
call "Note3.ls" import "@StartTime = 2.5" "@JudgeTime = 7.5"
call "Note4.ls" import "@StartTime = 7.5" "@JudgeTime = 12.5"
define @StartTime = 10
define @JudgeTime = 15 
define @HoldTime = 2
define @StartX = 0
define @EndX = 5
new(note1) Game.NoteGenerater()
note1 -> target -> InitPosition(@StartX,@StartY,@StartZ)
note1 -> target -> MakeMovement(@StartTime,@JudgeTime,@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note1 -> target -> MakeMovement(@JudgeTime,@SongEndTime,@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note1 -> target -> MakeScale(0,@StartTime,0,0,0,0,0,0,0)
note1 -> target -> MakeScale(@StartTime,@JudgeTime,1,1,1,1,1,1,0)
note1 -> target -> MakeScale(@JudgeTime,@SongEndTime,0,0,0,0,0,0,0)
note1 -> target -> InitNote(@JudgeTime,@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note1 -> target -> RegisterOnTimeLine()
define @StartY = 2
define @StartX = 2
define @EndX = 5
new(note2) Game.NoteGenerater()
note2 -> target -> InitPosition(@StartX,@StartY,@StartZ)
note2 -> target -> MakeMovement("@HoldTime+@StartTime","@HoldTime+@JudgeTime",@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note2 -> target -> MakeMovement("@HoldTime+@JudgeTime","@HoldTime+@SongEndTime",@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note2 -> target -> MakeScale(0,"@HoldTime+@StartTime",0,0,0,0,0,0,0)
note2 -> target -> MakeScale("@HoldTime+@StartTime","@HoldTime+@JudgeTime",1,1,1,0,0,0,0)
note2 -> target -> MakeScale("@HoldTime+@JudgeTime","@HoldTime+@SongEndTime",0,0,0,0,0,0,0)
note2 -> target -> InitNote("@HoldTime+@JudgeTime",@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note2 -> target -> RegisterOnTimeLine()
define @StartX = 5
define @EndX = 5
define @StartY = 0
define @HoldTime = 2.5
new(note3) Game.NoteGenerater()
note3 -> target -> InitPosition(@StartX,@StartY,@StartZ)
note3 -> target -> MakeMovement("@HoldTime+@StartTime","@HoldTime+@JudgeTime",@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)
note3 -> target -> MakeMovement("@HoldTime+@JudgeTime","@HoldTime+@SongEndTime",@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)
note3 -> target -> MakeScale(0,"@HoldTime+@StartTime",0,0,0,0,0,0,0)
note3 -> target -> MakeScale("@HoldTime+@StartTime","@HoldTime+@JudgeTime",1,1,1,1,1,1,0)
note3 -> target -> MakeScale("@HoldTime+@JudgeTime","@HoldTime+@SongEndTime",0,0,0,0,0,0,0)
note3 -> target -> InitNote("@HoldTime+@JudgeTime",@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)
note3 -> target -> RegisterOnTimeLine()
new(long) Game.LongNoteBodyGenerater()
long -> target -> AddNoteGenerater(note1)
long -> target -> AddNoteGenerater(note2)
long -> target -> AddNoteGenerater(note3)
long -> target -> MakeRebuildInterval("@StartTime-0.1","@HoldTime+@JudgeTime+0.1")
long -> target -> RegisterOnTimeLine()
