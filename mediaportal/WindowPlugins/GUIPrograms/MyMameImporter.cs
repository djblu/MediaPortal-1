/* 
 *	Copyright (C) 2005 Media Portal
 *	http://mediaportal.sourceforge.net
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using MediaPortal.GUI.Library;
using Programs.Utils;
using SQLite.NET;
using WindowPlugins.GUIPrograms;

namespace ProgramsDatabase
{
  /// <summary>
  /// Summary description for MyMameImporter.
  /// </summary>
  public class MyMameImporter
  {
    AppItem curApp = null;
    SQLiteClient sqlDB = null;

    // event: read new file
    public delegate void MyEventHandler(string strLine, int curPos, int maxPos);

    public event MyEventHandler OnReadNewFile = null;
    public event MyEventHandler OnSendMessage = null;
    ProgramConditionChecker Checker = new ProgramConditionChecker();
    string mameDir;
    string catverIniFile;
    string historyDatFile;
    StringCollection listFull = new StringCollection();
    StringCollection listGames = new StringCollection();
    StringCollection catverIni = new StringCollection();
    StringCollection historyDat = new StringCollection();
    StringCollection cacheRomnames = new StringCollection();
    StringCollection cacheHistoryRomnames = new StringCollection();
    string[] mameRoms;

    public MyMameImporter(AppItem objApp, SQLiteClient objDB)
    {
      curApp = objApp;
      sqlDB = objDB;
    }

    void ReadFileFromStream(string filename, StringCollection coll)
    {
      string line;
      coll.Clear();
      StreamReader sr = File.OpenText(filename);
      while (true)
      {
        line = sr.ReadLine();
        if (line == null)
        {
          break;
        }
        else
        {
          coll.Add(line);
        }
      }
      sr.Close();
    }


    void ReadListFull()
    {
      string line;
      string rom;
      int romendpos;
      listFull.Clear();
      cacheRomnames.Clear();

      Process myProcess = new Process();
      ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(curApp.Filename);
      if (((appItemMameDirect)curApp).ImportOriginalsOnly)
      {
        myProcessStartInfo.Arguments = "-listfull -noclones";
      }
      else
      {
        myProcessStartInfo.Arguments = "-listfull";
      }
      myProcessStartInfo.UseShellExecute = false;
      myProcessStartInfo.RedirectStandardOutput = true;
      myProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
      myProcessStartInfo.CreateNoWindow = true;
      myProcess.StartInfo = myProcessStartInfo;
      myProcess.Start();

      StreamReader sr = myProcess.StandardOutput;
      while (true)
      {
        line = sr.ReadLine();
        if (line == null)
        {
          break;
        }
        else
        {
          listFull.Add(line);
          romendpos = line.IndexOf(" ");
          rom = line.Substring(0, romendpos);
          cacheRomnames.Add(rom);
        }
      }

      myProcess.Close();
    }

    void ReadListGames()
    {
      string line;
      Process myProcess = new Process();
      ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(curApp.Filename);
      if (((appItemMameDirect)curApp).ImportOriginalsOnly)
      {
        myProcessStartInfo.Arguments = "-listgames -noclones";
      }
      else
      {
        myProcessStartInfo.Arguments = "-listgames";
      }
      myProcessStartInfo.UseShellExecute = false;
      myProcessStartInfo.RedirectStandardOutput = true;
      myProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
      myProcessStartInfo.CreateNoWindow = true;
      myProcess.StartInfo = myProcessStartInfo;
      myProcess.Start();
      StreamReader sr = myProcess.StandardOutput;
      while (true)
      {
        line = sr.ReadLine();
        if (line == null)
        {
          break;
        }
        else
        {
          listGames.Add(line);
        }
      
      }
      myProcess.Close();
    }


    void ReadHistoryDat()
    {
      string line;
      historyDat.Clear();
      cacheHistoryRomnames.Clear();
      StreamReader sr = File.OpenText(historyDatFile);
      ArrayList roms;
      int linenum = -1;
      while (true)
      {
        line = sr.ReadLine();
        if (line == null)
        {
          break;
        }
        else
        {
          if (line != "")
          {
            historyDat.Add(line);
          }
          if (line.StartsWith("$info="))
          {
            roms = new ArrayList(line.Substring(6).Split(','));
            foreach (string curRom in roms)
            {
              if (curRom != "")
              {
                linenum = historyDat.Count - 1;
                cacheHistoryRomnames.Add(curRom + "#" + linenum.ToString());
              }
            }
          }
        }
      }
    }


    bool CheckPrerequisites()
    {
      Checker.Clear();
      Checker.DoCheck(System.IO.Directory.Exists(curApp.FileDirectory), "rom-directory doesn't exist!");
      if (Checker.DoCheck(System.IO.File.Exists(curApp.Filename), "mame-application not found!"))
      {
        mameDir = Path.GetDirectoryName(curApp.Filename);
        catverIniFile = mameDir + "\\catver.ini";
        historyDatFile = mameDir + "\\history.dat";
      }

      if (Checker.IsOk)
      {
        SendText("generating mame-gamelist");
        ReadListFull();
        ReadListGames();
        if (System.IO.File.Exists(catverIniFile))
        {
          SendText("reading catver.ini");
          ReadFileFromStream(catverIniFile, catverIni);
        }
        if (System.IO.File.Exists(historyDatFile))
        {
          SendText("reading history.dat");
          ReadHistoryDat();
        }
        mameRoms = System.IO.Directory.GetFiles(curApp.FileDirectory, "*.zip");
      }
      return Checker.IsOk;
    }

    void SendText(string msg)
    {
      if (OnSendMessage != null)
      {
        OnSendMessage(msg, -1, -1);
      }
    }

    public void Start()
    {
      if (!CheckPrerequisites())
      {
        OnSendMessage(Checker.Problems, -1, -1);
        Log.Write("MameImporter: import failed! Details: {0}", Checker.Problems);
        return;
      }
      int i = 0;
      foreach (string fileName in mameRoms)
      {
        FillFileItem(fileName, i);
        i++;
      }
    }

    int GetLinePos(StringCollection coll, string startOfLine, int startPos)
    {
      int res = -1;
      for (int i = startPos; i < coll.Count; i++)
      {
        if (coll[i].StartsWith(startOfLine))
        {
          res = i;
          break;
        }
      }
      return res;
    }

    string GetHistory(int index)
    {
      string res = "";
      string sep = "";
      string line;
      bool skipping = true;
      while (true)
      {
        line = this.historyDat[index];
        if (line.StartsWith("$end"))
        {
          break;
        }
        if (!skipping)
        {
          if ((res != "") || (line != ""))
          {
            res = res + sep + line;
            sep = "\r\n";
          }
        }
        if (line.StartsWith("$bio"))
        {
          skipping = false;
        }
        index++;
        if (index >= historyDat.Count)
        {
          break;
        }
      }
      return res;
    }

    void FillFileItem(string fullRomname, int count)
    {
      string curRomname = Path.GetFileNameWithoutExtension(fullRomname).ToLower();
      string fullEntry = "";
      string gameEntry = "";
      string genreEntry = "";
      string versionEntry = "";
      string history = "";
      int historyIndex = -1;
      FileItem curFile = new FileItem(sqlDB);
      curFile.AppID = curApp.AppID;
      curFile.Filename = fullRomname;
      curFile.Imagefile = GetImageFile(curRomname);
      if ((curFile.Imagefile == "") && (curApp.ImportValidImagesOnly))
      {
        return;
      }
      int linePos = cacheRomnames.IndexOf(curRomname);
      if (linePos > 0)
      {
        fullEntry = listFull[linePos]; //  mspacman  "Ms. Pac-Man"
        gameEntry = listGames[linePos - 1]; // 1981 Midway   Ms. Pac-Man
        linePos = GetLinePos(catverIni, curRomname + "=", 0);
        if (linePos >= 0)
        {
          genreEntry = catverIni[linePos]; // mspacman=Maze
          linePos = GetLinePos(catverIni, curRomname + "=", linePos + 1);
          if (linePos >= 0)
          {
            versionEntry = catverIni[linePos]; //mspacman=.37b16
          }
        }
        historyIndex = GetHistoryIndex(curRomname);
        if (historyIndex != -1)
        {
          history = GetHistory(historyIndex); // multiline overview of the game
        }

        ProcessFullEntry(curFile, fullEntry);
        ProcessGameEntry(curFile, gameEntry);
        ProcessGenreEntry(curFile, genreEntry);
        ProcessVersionEntry(curFile, versionEntry);
        curFile.System_ = "Arcade";
        curFile.Rating = 5;
        curFile.Overview = history;
        curFile.Write();
        if (OnReadNewFile != null)
        {
          OnReadNewFile(curFile.Title, count, mameRoms.Length);
        }

      }
    }

    int GetHistoryIndex(string rom)
    {
      // locate one rom in the lookup table
      // format of the entries: <romname>#<linenumber>
      int res = -1;
      int historyPos = GetLinePos(cacheHistoryRomnames, rom + "#", 0);
      if (historyPos != -1)
      {
        string historyEntry = cacheHistoryRomnames[historyPos];
        ArrayList temp = new ArrayList(historyEntry.Split('#'));
        if (temp.Count > 1)
        {
          res = ProgramUtils.StrToIntDef(temp[1].ToString(),  -1);
        }
      }
      return res;
    }

    void ProcessFullEntry(FileItem curFile, string fullEntry)
    {
      //  mspacman  "Ms. Pac-Man"
      ArrayList temp = new ArrayList(fullEntry.Split('"'));
      if (temp.Count == 3)
      {
        curFile.Title = temp[1].ToString();
      }
      else
      {
        Log.Write("myPrograms: mameImport(ProcessFullEntry): unexpected string '{0}'", fullEntry);
      }
    }

    void ProcessGameEntry(FileItem curFile, string gameEntry)
    {
      if (gameEntry != "")
      {
        // 1981 Midway   Ms. Pac-Man
        string strYear = gameEntry.Substring(0, 4).Trim();
        string strManu = gameEntry.Substring(5, 37).Trim();
        curFile.Year = ProgramUtils.StrToIntDef(strYear, -1);
        curFile.Manufacturer = strManu;
      }
    }

    void ProcessGenreEntry(FileItem curFile, string genreEntry)
    {
      if (genreEntry != "")
      {
        // mspacman=Maze
        ArrayList temp = new ArrayList(genreEntry.Split('='));
        if (temp.Count == 2)
        {
          string allGenres = temp[1].ToString();
          ArrayList temp2 = new ArrayList(allGenres.Split('/'));
          if (temp2.Count > 0)
          {
            curFile.Genre = temp2[0].ToString().Trim();
          }
          if (temp2.Count > 1)
          {
            curFile.Genre2 = temp2[1].ToString().Trim();
          }
          if (temp2.Count > 2)
          {
            curFile.Genre3 = temp2[2].ToString().Trim();
          }
          if (temp2.Count > 3)
          {
            curFile.Genre4 = temp2[3].ToString().Trim();
          }
          if (temp2.Count > 4)
          {
            curFile.Genre5 = temp2[4].ToString().Trim();
          }
        }
        else
        {
          Log.Write("myPrograms: mameImport(ProcessGenreEntry): unexpected string '{0}'", genreEntry);
        }
      }
      
    }

    void ProcessVersionEntry(FileItem curFile, string versionEntry)
    {
      if (versionEntry != "")
      {
        //mspacman=.37b16
        ArrayList temp = new ArrayList(versionEntry.Split('='));
        if (temp.Count == 2)
        {
          curFile.CategoryData = String.Format("version={0}", temp[1].ToString());
        }
        else
        {
          Log.Write("myPrograms: mameImport(ProcessVersionEntry): unexpected string '{0}'", versionEntry);
        }
      }
      
    }

    string GetImageFile(string curRomname)
    {
      string res = "";
      string imgFolder = "";
      int i = 0;
      while ((res == "") && (i < curApp.imageDirs.Length))
      {
        imgFolder = curApp.imageDirs[i];
        res = GetImageFileOfFolder(curRomname, imgFolder);
        i++;
      }
      return res;
    }

    string GetImageFileOfFolder(string curRomname, string imgFolder)
    {
      string res = "";
      if (Directory.Exists(imgFolder))
      {
        string filenameNoExtension = imgFolder + "\\" + curRomname;
        if (File.Exists(Path.ChangeExtension(filenameNoExtension, ".png")))
        {
          res = Path.ChangeExtension(filenameNoExtension, ".png");
        }
        else if (File.Exists(Path.ChangeExtension(filenameNoExtension, ".jpg")))
        {
          res = Path.ChangeExtension(filenameNoExtension, ".jpg");
        }
        else if (File.Exists(Path.ChangeExtension(filenameNoExtension, ".gif")))
        {
          res = Path.ChangeExtension(filenameNoExtension, ".gif");
        }
      }
      return res;
    }

  }
}