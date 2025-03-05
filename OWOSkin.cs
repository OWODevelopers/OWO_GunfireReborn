using OWOGame;
using System.Net;

namespace OWO_GunfireReborn
{

    public class OWOSkin
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        private static bool heartBeatIsActive = false;
        private static bool ChargingWeaponIsActive = false;
        private static bool ChargingWeaponRIsActive = false;
        private static bool ChargingWeaponLIsActive = false;
        private static bool ContinueWeaponIsActive = false;
        private static bool ContinueWeaponRIsActive = false;
        private static bool ContinueWeaponLIsActive = false;
        private static bool CloudWeaverIsActive = false;
        private static bool CloudWeaverRIsActive = false;
        private static bool CloudWeaverLIsActive = false;
        private static bool qianPrimarySkillIsActive = false;
        private static bool liPrimarySkillIsActive = false;
        private static bool taoPrimarySkillIsActive = false;

        public Dictionary<String, Sensation> FeedbackMap = new Dictionary<string, Sensation>();

        public OWOSkin()
        {
            RegisterAllSensationsFiles();
            InitializeOWO();
        }

        ~OWOSkin()
        {
            LOG("Destructor called");
            DisconnectOWO();
        }

        public void DisconnectOWO()
        {
            LOG("Disconnecting OWO skin.");
            OWO.Disconnect();
        }

        public void LOG(string logStr)
        {
            Plugin.Log.LogMessage(logStr);
        }

        private void RegisterAllSensationsFiles()
        {
            string configPath = Directory.GetCurrentDirectory() + "\\BepinEx\\Plugins\\OWO";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.owo", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    Sensation test = Sensation.Parse(tactFileStr);
                    FeedbackMap.Add(prefix, test);
                }
                catch (Exception e) { LOG(e.Message); }

            }

            systemInitialized = true;
        }

        private async void InitializeOWO()
        {
            LOG("Initializing OWO skin");

            var gameAuth = GameAuth.Create(AllBakedSensations()).WithId("67884529");

            OWO.Configure(gameAuth);
            string[] myIPs = GetIPsFromFile("OWO_Manual_IP.txt");
            if (myIPs.Length == 0) await OWO.AutoConnect();
            else
            {
                await OWO.Connect(myIPs);
            }

            if (OWO.ConnectionState == OWOGame.ConnectionState.Connected)
            {
                suitDisabled = false;
                LOG("OWO suit connected.");
                Feel("Heart Beat");
            }
            if (suitDisabled) LOG("OWO is not enabled?!?!");
        }

        public BakedSensation[] AllBakedSensations()
        {
            var result = new List<BakedSensation>();

            foreach (var sensation in FeedbackMap.Values)
            {
                if (sensation is BakedSensation baked)
                {
                    LOG("Registered baked sensation: " + baked.name);
                    result.Add(baked);
                }
                else
                {
                    LOG("Sensation not baked? " + sensation);
                    continue;
                }
            }
            return result.ToArray();
        }

        public string[] GetIPsFromFile(string filename)
        {
            List<string> ips = new List<string>();
            string filePath = Directory.GetCurrentDirectory() + "\\BepinEx\\Plugins\\OWO" + filename;
            if (File.Exists(filePath))
            {
                LOG("Manual IP file found: " + filePath);
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    if (IPAddress.TryParse(line, out _)) ips.Add(line);
                    else LOG("IP not valid? ---" + line + "---");
                }
            }
            return ips.ToArray();
        }

        public void Feel(String key, int Priority = 0, float intensity = 1.0f, float duration = 1.0f)
        {
            LOG("SENSATION: " + key);

            if (FeedbackMap.ContainsKey(key))
            {
                OWO.Send(FeedbackMap[key].WithPriority(Priority));
            }

            else LOG("Feedback not registered: " + key);
        }

        #region HeartBeat

        public async Task HeartBeatFuncAsync()
        {
            while (heartBeatIsActive)
            {
                Feel("Heart Beat", 0);
                await Task.Delay(1000);
            }
        }
        public void StartHeartBeat()
        {
            if (heartBeatIsActive) return;

            heartBeatIsActive = true;
            HeartBeatFuncAsync();
        }

        public void StopHeartBeat()
        {
            heartBeatIsActive = false;
        }

        #endregion

        #region CloudWeaver

        public async Task CloudWeaverFuncAsync()
        {
            string toFeel = "";
            while (CloudWeaverLIsActive || CloudWeaverRIsActive)
            {
                if (CloudWeaverRIsActive)
                    toFeel = "Cloud Weaver R";

                if (CloudWeaverLIsActive)
                    toFeel = "Cloud Weaver L";

                if (CloudWeaverLIsActive && CloudWeaverRIsActive)
                    toFeel = "Cloud Weaver LR";

                Feel(toFeel, 2);
                await Task.Delay(1000);
            }
            CloudWeaverIsActive = false;
        }
        public void StartCloudWeaver(bool isRight)
        {
            if (isRight)
                CloudWeaverRIsActive = true;

            if (!isRight)
                CloudWeaverLIsActive = true;

            if (!CloudWeaverIsActive)
                CloudWeaverFuncAsync();

            CloudWeaverIsActive = true;
        }

        public void StopCloudWeaver(bool isRight)
        {
            if (isRight)
            {
                CloudWeaverRIsActive = false;
            }
            else
            {
                CloudWeaverLIsActive = false;
            }

            OWO.Stop();
        }

        #endregion

        #region ChargingWeapon

        public async Task ChargingWeaponFuncAsync()
        {
            string toFeel = "";
            while (ChargingWeaponLIsActive || ChargingWeaponRIsActive)
            {
                if (ChargingWeaponRIsActive)
                    toFeel = "Charging R";

                if (CloudWeaverLIsActive)
                    toFeel = "Charging L";

                if (ChargingWeaponLIsActive && ChargingWeaponRIsActive)
                    toFeel = "Charging LR";

                Feel(toFeel, 2);
                await Task.Delay(1000);
            }
            CloudWeaverIsActive = false;
        }
        public void StartChargingWeapon(bool isRight)
        {
            if (isRight)
                ChargingWeaponRIsActive = true;

            if (!isRight)
                ChargingWeaponLIsActive = true;

            if (!ChargingWeaponIsActive)
                ChargingWeaponFuncAsync();

            ChargingWeaponIsActive = true;
        }

        public void StopChargingWeapon(bool isRight)
        {
            if (isRight)
            {
                ChargingWeaponRIsActive = false;
            }
            else
            {
                ChargingWeaponLIsActive = false;
            }

            OWO.Stop();
        }

        #endregion

        #region ContinueWeapon

        public async Task ContinueWeaponFuncAsync()
        {
            string toFeel = "";
            while (ContinueWeaponLIsActive || ContinueWeaponRIsActive)
            {
                if (ContinueWeaponRIsActive)
                    toFeel = "Continuous R";

                if (CloudWeaverLIsActive)
                    toFeel = "Continuous L";

                if (ContinueWeaponLIsActive && ContinueWeaponRIsActive)
                    toFeel = "Continuous LR";

                Feel(toFeel, 2);
                await Task.Delay(400);
            }
            CloudWeaverIsActive = false;
        }
        public void StartContinueWeapon(bool isRight)
        {
            if (isRight)
                ContinueWeaponRIsActive = true;

            if (!isRight)
                ContinueWeaponLIsActive = true;

            if (!ContinueWeaponIsActive)
                ContinueWeaponFuncAsync();

            ContinueWeaponIsActive = true;
        }

        public void StopContinueWeapon(bool isRight)
        {
            if (isRight)
            {
                ContinueWeaponRIsActive = false;
            }
            else
            {
                ContinueWeaponLIsActive = false;
            }

            OWO.Stop();
        }

        #endregion

        #region TaoPrimarySkill

        public async Task TaoPrimarySkillFuncAsync()
        {
            while (taoPrimarySkillIsActive)
            {
                Feel("Tao 1st", 0);
                await Task.Delay(600);
            }
        }
        public void StartTaoPrimarySkill()
        {
            if (taoPrimarySkillIsActive) return;

            taoPrimarySkillIsActive = true;
            TaoPrimarySkillFuncAsync();
        }

        public void StopTaoPrimarySkill()
        {
            taoPrimarySkillIsActive = false;
        }

        #endregion

        #region QianPrimarySkill

        public async Task QianPrimarySkillFuncAsync()
        {
            while (qianPrimarySkillIsActive)
            {
                Feel("Qian 1st", 0);
                await Task.Delay(1000);
            }
        }
        public void StartQianPrimarySkill()
        {
            if (qianPrimarySkillIsActive) return;

            qianPrimarySkillIsActive = true;
            QianPrimarySkillFuncAsync();
        }

        public void StopTurtlePrimarySkill()
        {
            qianPrimarySkillIsActive = false;
        }

        #endregion

        #region LiPrimarySkill

        public async Task LiPrimarySkillFuncAsync()
        {
            while (liPrimarySkillIsActive)
            {
                Feel("Li 1st", 0);
                await Task.Delay(500);
            }
        }
        public void StartLiPrimarySkill()
        {
            if (liPrimarySkillIsActive) return;

            liPrimarySkillIsActive = true;
            LiPrimarySkillFuncAsync();
        }

        public void StopLiPrimarySkill()
        {
            liPrimarySkillIsActive = false;
        }

        #endregion

        public void StopAllHapticFeedback()
        {
            StopHeartBeat();
            StopTaoPrimarySkill();
            StopTurtlePrimarySkill();
            StopLiPrimarySkill();
            StopChargingWeapon(true);
            StopChargingWeapon(false);
            StopCloudWeaver(true);
            StopCloudWeaver(false);
            StopContinueWeapon(true);
            StopContinueWeapon(false);

            OWO.Stop();
        }

    }
}
