using ConsoleTables;
using Grand_Prix;
using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;

internal class Championship
{
    static void Main(string[] args)
    {
        // 读取场地信息
        var pathvenue = "venues.txt";
        ArrayList venues = ReadVenues(pathvenue);
        // 读取驾驶员信息
        var pathDriver = "driver.txt";
        var pathLog = "log.txt";
        File.WriteAllText(pathLog, string.Empty); // 清空文件
        List<Driver> drivers = ReadDrivers(pathDriver);

        // 开始比赛
        StartChampionship(venues, drivers, pathLog, pathDriver);
    }

    //开始比赛
    private static void StartChampionship(ArrayList venues, List<Driver> drivers, string pathLog, string pathDriver)
    {
        Console.WriteLine("请选择你要参加的比赛数目（至少参加三场，至多参加五场）：");
        while (true)
        {
            int n;
            try
            {
                n = int.Parse(Console.ReadLine());
                if (n < 3 || n > 5)
                {
                    Console.WriteLine("输入错误，请重新输入！");
                    continue;
                }

                for (int i = 0; i < n; i++)
                {
                    // 选择场地并开始比赛
                    Venue selectedVenue = SelectVenue(venues);
                    if (selectedVenue == null) continue;

                    Console.WriteLine("--比赛开始了--");
                    RunRace(drivers, venues, selectedVenue, pathLog);
                    AssignPointsAndRanking(drivers);
                    Console.WriteLine("--这场比赛结束了--");
                    Console.WriteLine();
                    PrintDriverInfo(drivers);
                }

                // 对积分进行排序
                ArrayList arrayList = new ArrayList();
                arrayList.AddRange(drivers);
                CalculateChampionshipResults(arrayList, pathDriver);
            }
            catch
            {
                Console.WriteLine("输入错误，请重新输入!!!");
            }
            Console.ReadLine();
        }
    }

    //选择场地
    private static Venue SelectVenue(ArrayList venues)
    {
        Venue thisVenue = null;
        int x = 0;
        Console.WriteLine("请选择比赛场地：");
        var table = new ConsoleTable("venueName", "number of turns", "averageLapTime", "chanceOfRain");

        foreach (Venue item in venues)
        {
            table.AddRow(item.venueName, item.noOfLaps, item.averageLapTime, item.chanceOfRain);
        }
        table.Write(Format.Alternative);

        while (true)
        {
            Console.WriteLine("你的选择：");
            string thisVenueName = Console.ReadLine(); // 获取比赛场地
            foreach (Venue item in venues)
            {
                if (thisVenueName == item.venueName)
                {
                    thisVenue = item;
                    x = 1;
                    File.AppendAllText("log.txt", item.venueName + "\r\n"); // 添加进文件
                }
            }
            if (x == 1) break;
            Console.WriteLine("输入错误，请重新输入");
        }

        return thisVenue;
    }

    //比赛过程
    private static void RunRace(List<Driver> drivers, ArrayList venues, Venue thisVenue, string pathLog)
    {
        int lapNum = thisVenue.noOfLaps; // 获取场地圈数
        double rainProbability = thisVenue.chanceOfRain * 100; // 下雨概率

        // 起跑时间需加上排名罚时
        ApplyPenaltyBasedOnRanking(drivers, pathLog);

        double adds = RandomNumber.isRain();
        for (int j = 1; j <= lapNum; j++) // 开始跑圈
        {
            File.AppendAllText(pathLog, j + "st lap");
            foreach (Driver item in drivers)
            {
                if (item.eligible)
                {
                    HandleMechanicalFailure(item, pathLog);
                    HandleSkills(item, j, pathLog);
                    HandleRain(item, j, rainProbability, adds, lapNum, pathLog);
                    item.accumulateTime += thisVenue.averageLapTime;
                    File.AppendAllText(pathLog, "\r\n");
                }
            }
            drivers.Sort(); // 跑完本圈之后按时间进行排序
            UpdateDriverRanking(drivers);

            Console.WriteLine($"第{j}圈结果如下：");
            PrintDriverLap(drivers);
            File.AppendAllText(pathLog, "\r\n\r\n");
        }

        RemoveVenue(venues, thisVenue);
    }

    //排名罚时
    private static void ApplyPenaltyBasedOnRanking(List<Driver> drivers, string pathLog)
    {
        foreach (Driver item in drivers)
        {
            int penalty;
            if (item.ranking == 1)
            {
                penalty = 0;
            }
            else if (item.ranking == 2)
            {
                penalty = 3;
            }
            else if (item.ranking == 3)
            {
                penalty = 5;
            }
            else if (item.ranking == 4)
            {
                penalty = 7;
            }
            else
            {
                penalty = 10;
            }

            item.accumulateTime += penalty;
            File.AppendAllText(pathLog, item.name + "罚时是" + item.accumulateTime + "秒\r\n");
        }
        File.AppendAllText(pathLog, "\r\n");
    }

    //机械故障
    private static void HandleMechanicalFailure(Driver item, string pathLog)
    {
        int time = RandomNumber.mechanicalFailure(); // 获取机械故障概率
        if (time == int.MaxValue)
        {
            File.AppendAllText(pathLog, item.name + "无法修复的机械故障，失去比赛资格!\r\n");
            item.eligible = false;
            item.accumulateTime = int.MaxValue;
        }
        else
        {
            item.accumulateTime += time; // 加上机械故障时间
            File.AppendAllText(pathLog, time == 20
                ? item.name + "轻微机械故障,增加20秒!\r\n"
                : item.name + "重大机械故障,增加120秒!\r\n");
        }
    }

    //技能使用
    private static void HandleSkills(Driver item, int lap, string pathLog)
    {
        if (item.specialskill.Equals("breaking") || item.specialskill.Equals("cornering"))
        {
            int skilltime = RandomNumber.BreakingAndCornering();
            item.accumulateTime -= skilltime;
            File.AppendAllText(pathLog, item.name + "使用" + item.specialskill + "减去" + skilltime + "秒\r\n");
            item.isskill = true;
        }
        else
        {
            int time1 = RandomNumber.Overtaking(lap);
            item.accumulateTime -= time1;
            if (time1 != 0)
            {
                item.isskill = true;
                File.AppendAllText(pathLog, item.name + "使用" + item.specialskill + "减去" + time1 + "秒\r\n");
            }
            else
            {
                File.AppendAllText(pathLog, item.name + "不能使用" + item.specialskill + "减去" + time1 + "秒\r\n");
            }
        }
    }

    //降雨概率
    private static void HandleRain(Driver item, int lap, double rainProbability, double adds, int lapNum, string pathLog)
    {
        if (lap == 2 && adds <= rainProbability)
        {
            File.AppendAllText(pathLog, "下雨了！");
            if (RandomNumber.changeTire() == 2)
            {
                item.accumulateTime += 10;
                File.AppendAllText(pathLog, item.name + "换轮胎了，加10秒\r\n");
            }
            else
            {
                item.accumulateTime += 5 * (lapNum - 1);
                File.AppendAllText(pathLog, item.name + $"没换轮胎，每圈加5秒,共加{5 * (lapNum - 1)}秒\r\n");
            }
        }
        else
        {
            File.AppendAllText(pathLog, "没下雨" + item.name + "没有罚时" + "\r\n");
        }
    }

    //更新排名
    private static void UpdateDriverRanking(List<Driver> drivers)
    {
        int k = 1;
        foreach (var driver in drivers)
        {
            driver.ranking = k++;
        }
    }

    //场地更换
    private static void RemoveVenue(ArrayList venues, Venue thisVenue)
    {
        foreach (Venue venue in venues)
        {
            if (venue.venueName.Equals(thisVenue.venueName))
            {
                venues.Remove(venue);
                break;
            }
        }
    }

    //成绩赋分
    public static void AssignPointsAndRanking(List<Driver> drivers)//跑完之后赋分
    {
        int count = 1;
        foreach (Driver driver in drivers)
        {
            if (count == 1)
            {
                driver.accumulateScore += 8;
            }
            else if (count == 2)
            {
                driver.accumulateScore += 5;
            }
            else if (count == 3)
            {
                driver.accumulateScore += 3;
            }
            else if (count == 4)
            {
                driver.accumulateScore += 1;
            }
            else
            {
                driver.accumulateScore += 0;
            }
            driver.ranking = count++;
        }

    }
    ////积分排名
    public static void CalculateChampionshipResults(ArrayList drivers, string pathDriver)
    {
        drivers.Sort(new ScoreCompare());
        Console.WriteLine("比赛的最终结果：");
        int rank1 = 1;
        File.WriteAllText(pathDriver, string.Empty);//清空文件
        var table = new ConsoleTable("ranking", "name", "Score");
        foreach (Driver driver in drivers)
        {
            driver.ranking = rank1++;
            table.AddRow(driver.ranking, driver.name, driver.accumulateScore);//向表格中添加一行数据
            string str = driver.name + ", " + driver.ranking + ", " + driver.specialskill + ", " + driver.eligible + ", " + 0 + "\r\n";//文件操作，创建一个字符串 str，包含驾驶员的姓名、排名、特殊技能、是否有资格参加比赛等信息
            File.AppendAllText(pathDriver, str);  //添加进文件
        }
        table.Write(Format.Alternative);
    }

    //读取车手信息
    public static List<Driver> ReadDrivers(string pathDriver)
    {
        string[] lines = File.ReadAllLines(pathDriver);
        string[] Str;

        List<Driver> drivers = new List<Driver>();

        foreach (string line in lines)
        {
            Driver driver = new Driver();
            Str = line.Split(',');
            driver.name = Str[0];
            driver.ranking = int.Parse(Str[1]);
            driver.specialskill = Str[2].Trim();

            if (Str[3].Trim().ToLower().Equals("true"))
            {
                driver.eligible = true;
            }
            else if (Str[3].Trim().ToLower().Equals("false"))
            {
                driver.eligible = false;
            }
            else
            {
                Console.WriteLine("Error!");
                return null;
            }

            driver.accumulateTime = int.Parse(Str[4]);
            driver.accumulateScore = 0;
            driver.isskill = false;
            drivers.Add(driver);
        }
        return drivers;
    }
    //读取场地信息
    public static ArrayList ReadVenues(string pathvenue)
    {
        ArrayList venues = new ArrayList();
        string[] lines = File.ReadAllLines(pathvenue);
        string[] Str;
        foreach (string line in lines)
        {
            Venue venue = new Venue();
            Str = line.Split(',');
            venue.venueName = Str[0];
            venue.noOfLaps = int.Parse(Str[1]);
            venue.averageLapTime = int.Parse(Str[2]);
            venue.chanceOfRain = double.Parse(Str[3]);
            venues.Add(venue);
        }
        return venues;
    }
    //输出比赛结果
    public static void PrintDriverInfo(List<Driver> drivers)
    {
        var table = new ConsoleTable("ranking", "name", "accumulateTime", "accumulateScore");
        foreach (Driver driver in drivers)
        {
            if (driver.eligible)
            {
                table.AddRow(driver.ranking, driver.name, driver.accumulateTime, driver.accumulateScore);
            }
            else
            {
                table.AddRow(driver.ranking, driver.name, "   lose qualification   ", driver.accumulateScore);
            }
            driver.eligible = true; //在下场比赛前使得所有人具备资格
            driver.accumulateTime = 0;//时间清零
        }
        table.Write(Format.Alternative);
        Console.WriteLine();
    }
    public static void PrintDriverLap(List<Driver> drivers)
    {
        var table = new ConsoleTable("ranking", "name", "accumulateTime", "accumulateScore", "skill", "isskill", "mechanical failure");
        foreach (Driver driver in drivers)
        {
            if (driver.eligible)
            {
                table.AddRow(driver.ranking, driver.name, driver.accumulateTime, driver.accumulateScore, driver.specialskill, driver.isskill, driver.Default);
            }
            else
            {
                table.AddRow(driver.ranking, driver.name, "     ", driver.accumulateScore, "Unrecoverable mechanical fault!lose qualification!", "     ", "  ");
            }
        }
        table.Write(Format.Alternative);
        Console.WriteLine();
    }

    class ScoreCompare : IComparer
    {
        public int Compare(object x, object y)
        {
            Driver player1 = (Driver)x;
            Driver player2 = (Driver)y;
            return player2.accumulateScore.CompareTo(player1.accumulateScore);
        }
    }
}


