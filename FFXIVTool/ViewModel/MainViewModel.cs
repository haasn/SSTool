﻿using FFXIVTool.Utility;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace FFXIVTool.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
    {
        private static Mediator mediator;

        private static BackgroundWorker worker;
        public Mem MemLib = new Mem();
        public static int gameProcId = 0;
        public static ThreadWriting ThreadTime;
        private static CharacterDetailsViewModel characterDetails;

		public event PropertyChangedEventHandler PropertyChanged;

		public CharacterDetailsViewModel CharacterDetails { get => characterDetails; set => characterDetails = value; }

		public PackIconKind AOTToggleStatus { get; set; } = PackIconKind.ToggleSwitchOffOutline;
		public Brush ToggleForeground { get; set; } = new SolidColorBrush(Color.FromArgb(0x75, 0xFF, 0xFF, 0xFF));

		public MainViewModel()
        {
            mediator = new Mediator();
            MemoryManager.Instance.MemLib.OpenProcess(gameProcId);
            LoadSettings();
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
            characterDetails = new CharacterDetailsViewModel(mediator);
            CharacterDetailsViewModel.baseAddr = MemoryManager.Add(MemoryManager.Instance.BaseAddress, "8");
            Task.Delay(40).Wait();
            ThreadTime = new ThreadWriting(); // Thread Writing
        }
        public static void ShutDownStuff()
        {
            worker.CancelAsync();
            ThreadTime.worker.CancelAsync();
            characterDetails = null;
            mediator = null;
            ThreadTime = null;
        }
        private void LoadSettings()
        {
            // create an xml serializer
            var serializer = new XmlSerializer(typeof(Settings), "");
            // create namespace to remove it for the serializer
            var ns = new XmlSerializerNamespaces();
            // add blank namespaces
            ns.Add("", "");
            using (var reader = new StreamReader(@"./OffsetSettings.xml"))
            {
                try
                {
                    Settings.Instance = (Settings)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            if (CheckUpdate()==true)
            {
                System.Windows.MessageBox.Show("We successfully updated offsets automatically for you! Please Restart the program or Press Find New Process on the top right of the application if this doesn't work!", "Oh wow!");
            }
        }
        private bool CheckUpdate()
        {
            try
            {
                string xmlStr;
                ServicePointManager.SecurityProtocol = (ServicePointManager.SecurityProtocol & SecurityProtocolType.Ssl3) | (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);
                using (var HAH = new WebClient())
                {
                    xmlStr = HAH.DownloadString(@"https://raw.githubusercontent.com/imchillin/SSTool/master/FFXIVTool/OffsetSettings.xml");
                }
                var serializer = new XmlSerializer(typeof(Settings), "");
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlStr);
                var offset = (Settings)serializer.Deserialize(new StringReader(xmlDoc.InnerXml));
                if (!string.Equals(Settings.Instance.LastUpdated, offset.LastUpdated))
                {
                    File.WriteAllText(@"./OffsetSettings.xml", xmlDoc.InnerXml);
                    Settings.Instance = offset;
                    return true;
                }
                else return false;
            }
            catch
            {
                return false;
            }
        }
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // no fancy tricks here boi
            MemoryManager.Instance.BaseAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.AoBOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.TargetAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.TargetOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.CameraAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.CameraOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.GposeAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.GposeOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.TimeAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.TimeOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.WeatherAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.WeatherOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.TerritoryAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.TerritoryOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.MusicOffset = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.MusicOffset, NumberStyles.HexNumber));
            MemoryManager.Instance.GposeFilters = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.GposeFilters, NumberStyles.HexNumber));
            MemoryManager.Instance.CharacterRenderAddress = MemoryManager.Instance.GetBaseAddress(int.Parse(Settings.Instance.CharacterRenderOffset, NumberStyles.HexNumber));
            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                }
                // sleep for 50 ms
                Thread.Sleep(50);
                // check if our memory manager is set /saving
                if (!MainWindow.CurrentlySaving) mediator.SendWork();
            }
        }

		public void ToggleStatus(bool on)
		{
			ToggleForeground = on ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(0x75, 0xFF, 0xFF, 0xFF));
			AOTToggleStatus = on ? PackIconKind.ToggleSwitch : PackIconKind.ToggleSwitchOffOutline;
		}
    }
}
