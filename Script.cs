IMyProgrammableBlock _missilePB;
string _missileCustomID;

IMyThrust _booster;
IMySensorBlock _proximitySensor;
IMyOffensiveCombatBlock _combatAI;
IMyFlightMovementBlock _movementAI;
IMyTextPanel _lcd;
IMyBatteryBlock _missileBattery;
IMyEventControllerBlock _MissileEventDistance;
IMyGyro _missileGyro;
IMyGasTank _missileFuelTank;
IMyShipConnector _missileConnector;
IMyShipMergeBlock _missileMerge;
IMyRadioAntenna _missileAntenna;
List<IMyThrust> _navThrust = new List<IMyThrust>();
List<IMyLightingBlock> _missileLights = new List<IMyLightingBlock>();
List<IMyWarhead> _warheads = new List<IMyWarhead>();

bool _setupcheck = true;
MissileStatusEnum _MissileStatus;

public Program()
{
    Echo("Initialising...");
    if (Storage.Length == 0 || Storage.Split('|').Length < 3) Storage = " | | ";
    _missilePB = Me;
    _missileCustomID = _missilePB.EntityId.ToString();
    Me.CubeGrid.CustomName = "Missile 0.2 " + _missileCustomID;
    List<IMyFunctionalBlock> functionalBlocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType(functionalBlocks, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(_warheads, b => b.CubeGrid == Me.CubeGrid);
    functionalBlocks.ForEach(x => x.CustomName = $"Missile {_missileCustomID.Remove(5)} {x.GetType().Name.Replace("My", "")}");
    _warheads.ForEach(x => x.CustomName = $"Missile {_missileCustomID.Remove(5)} {x.GetType().Name.Replace("My", "")}");

    Echo("Setting up blocks...");
    foreach (IMyFunctionalBlock block in functionalBlocks)
    {
        if (block is IMySensorBlock) _proximitySensor = block as IMySensorBlock;
        else if (block is IMyBatteryBlock) _missileBattery = block as IMyBatteryBlock;
        else if (block is IMyOffensiveCombatBlock) _combatAI = block as IMyOffensiveCombatBlock;
        else if (block is IMyFlightMovementBlock) _movementAI = block as IMyFlightMovementBlock;
        else if (block is IMyGyro) _missileGyro = block as IMyGyro;
        else if (block is IMyGasTank) _missileFuelTank = block as IMyGasTank;
        else if (block is IMyTextPanel) { _lcd = block as IMyTextPanel; _lcd.ContentType = ContentType.TEXT_AND_IMAGE; }
        else if (block is IMyEventControllerBlock) _MissileEventDistance = block as IMyEventControllerBlock;
        else if (block is IMyLightingBlock) _missileLights.Add(block as IMyLightingBlock);
        else if (block is IMyShipMergeBlock) _missileMerge = block as IMyShipMergeBlock;
        else if (block is IMyShipConnector) _missileConnector = block as IMyShipConnector;
        else if (block is IMyRadioAntenna) _missileAntenna = block as IMyRadioAntenna;
        else if (block is IMyThrust)
        {
            if (_movementAI == null) _movementAI = functionalBlocks.Find(x => x is IMyFlightMovementBlock) as IMyFlightMovementBlock;
            if (Base6Directions.GetOppositeDirection(block.Orientation.Forward) == _movementAI.Orientation.Forward)
                _booster = block as IMyThrust;
            _navThrust.Add(block as IMyThrust);
        }
        else if (block.GetType().Name == "MyExhaustBlock" && _MissileStatus != MissileStatusEnum.Idle) block.Enabled = true;
    }

    _setupcheck = StatusCheck();
    if (!_setupcheck) { Echo("Setup failed!"); return; }

    if (Storage.Length == 0) _MissileStatus = MissileStatusEnum.Idle;
    else _MissileStatus = MissileStatusStorage;

    MissileFlightCheck = _setupcheck;
    Me.CustomData = $"Flight Ready: {_setupcheck}\nStatus: {_MissileStatus}";

    Echo("All good!");
}

bool StatusCheck()
{
    Dictionary<string, bool> check = new Dictionary<string, bool>()
            {
                {"Proximity sensor",  _proximitySensor != null },
                {"Battery", _missileBattery != null },
                {"Combat AI", _combatAI != null },
                {"Moviment AI", _movementAI!=null},
                {"Gyroscope", _missileGyro!=null },
                {"Fuel Tank", _missileFuelTank!= null },
                {"Forward truster", _booster != null },
                {"Event Controller", _MissileEventDistance!= null },
                {"Merge block", _missileMerge!=null },
                {"Connector", _missileConnector!=null },
                {"Thrusters", _navThrust!= null && _navThrust.Count >= 6 },
                {"Warheads", _warheads!=null && _warheads.Count > 0}
            };
    bool temp = !check.Values.Contains(false);
    if (!temp) Echo($"{check.FirstOrDefault(x => x.Value == false).Key} was ither not found or was to few of it.");
    return temp;
}

void ConfigureMissile()
{
    _lcd?.WriteText("Configuering missile... ", true);
    int i = 0;
    _missileLights?.ForEach(x =>
    {
        x.BlinkIntervalSeconds = 2;
        x.BlinkLength = 0.1f;
        x.BlinkOffset = i++ / 10;
    });

    switch (_MissileStatus)
    {
        case MissileStatusEnum.Idle:
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                _lcd?.WriteText("Idle.\n", true);
                _missileConnector.Enabled = true;
                _missileConnector.Connect();
                _missileMerge.Enabled = true;

                _booster.ThrustOverride = 0;
                _navThrust.ForEach(x => x.Enabled = false);
                _proximitySensor.Enabled = false;
                _missileLights.ForEach(x => x.Enabled = false);
                _combatAI.Enabled = false;
                _movementAI.Enabled = false;
                _missileFuelTank.Stockpile = true;
                _missileBattery.ChargeMode = ChargeMode.Recharge;
                _warheads.ForEach(x => x.IsArmed = false);

                _proximitySensor.BackExtend = 50;
                _proximitySensor.FrontExtend = 50;
                _proximitySensor.RightExtend = 50;
                _proximitySensor.LeftExtend = 50;
                _proximitySensor.TopExtend = 50;
                _proximitySensor.BottomExtend = 50;
                _proximitySensor.DetectEnemy = false;
                _proximitySensor.DetectFriendly = true;
                _proximitySensor.DetectNeutral = true;
                _proximitySensor.DetectSubgrids = true;
                _proximitySensor.DetectStations = true;
                _proximitySensor.DetectOwner = true;
                _proximitySensor.DetectSmallShips = true;
                _proximitySensor.DetectLargeShips = true;
                _proximitySensor.DetectPlayers = false;

                if (_missileAntenna != null) _missileAntenna.Enabled = false;

                return;
            }
        case MissileStatusEnum.Deploying:
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                _lcd?.WriteText("Deploying.\n", true);
                _missileBattery.ChargeMode = ChargeMode.Auto;
                _missileFuelTank.Stockpile = false;
                _missileConnector.Disconnect();
                _missileMerge.Enabled = false;

                _booster.Enabled = true;
                _booster.ThrustOverridePercentage = 1;

                _proximitySensor.Enabled = true;
                _proximitySensor.BackExtend = 50;
                _proximitySensor.FrontExtend = 50;
                _proximitySensor.RightExtend = 50;
                _proximitySensor.LeftExtend = 50;
                _proximitySensor.TopExtend = 50;
                _proximitySensor.BottomExtend = 50;
                _proximitySensor.DetectEnemy = false;
                _proximitySensor.DetectFriendly = true;
                _proximitySensor.DetectNeutral = true;
                _proximitySensor.DetectSubgrids = true;
                _proximitySensor.DetectStations = true;
                _proximitySensor.DetectOwner = true;
                _proximitySensor.DetectSmallShips = true;
                _proximitySensor.DetectLargeShips = true;

                _combatAI.Enabled = false;
                _movementAI.Enabled = false;

                if (_missileAntenna != null) _missileAntenna.Enabled = true;
                _missileLights.ForEach(x => { x.Enabled = true; x.Color = Color.Yellow; });
                return;
            }
        case MissileStatusEnum.Approaching:
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                _lcd?.WriteText("Approaching.\n", true);
                _booster.ThrustOverride = 0;

                _combatAI.Enabled = true;
                _combatAI.SelectedAttackPattern = 3; //Intercept
                _combatAI.TargetPriority = OffensiveCombatTargetPriority.Closest;

                _movementAI.CollisionAvoidance = true;
                _movementAI.Enabled = true;
                _movementAI.PrecisionMode = false;
                _movementAI.SpeedLimit = 100;

                _navThrust.ForEach(x => x.Enabled = true);
                _warheads.ForEach(x => x.IsArmed = true);

                _MissileEventDistance.Enabled = true;
                _MissileEventDistance.Threshold = 200;


                _proximitySensor.Enabled = false;
                _missileLights.ForEach(x => { x.Enabled = true; x.Color = Color.Red; });

                //TODO: Find way to activate AI behavior

                return;
            }
        case MissileStatusEnum.Engaging:
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                _lcd?.WriteText("Engaging.\n", true);

                _movementAI.CollisionAvoidance = false;
                _MissileEventDistance.Threshold = 5;

                _proximitySensor.Enabled = true;
                _proximitySensor.BackExtend = 1.5f;
                _proximitySensor.FrontExtend = 1.5f;
                _proximitySensor.RightExtend = 1.5f;
                _proximitySensor.LeftExtend = 1.5f;
                _proximitySensor.TopExtend = 7f;
                _proximitySensor.BottomExtend = 2f;
                _proximitySensor.DetectEnemy = true;
                _proximitySensor.DetectFriendly = false;
                _proximitySensor.DetectNeutral = false;
                _proximitySensor.DetectSubgrids = false;
                _proximitySensor.DetectStations = true;
                _proximitySensor.DetectOwner = false;
                _proximitySensor.DetectSmallShips = true;
                _proximitySensor.DetectLargeShips = true;
                return;
            }

    }
}


void Operate(MissileCommandEnum command)
{
    if (ShouldDetonate()) { _warheads.ForEach(x => x.Detonate()); return; }

    _lcd?.WriteText("Operating missile... ", true);
    switch (command)
    {
        case MissileCommandEnum.Idle:
            {
                _lcd?.WriteText("Idle.\n", true);
                _MissileStatus = MissileStatusEnum.Idle;
                ConfigureMissile();
                return;
            }
        case MissileCommandEnum.Engage:
            {
                _lcd?.WriteText("Engage.\n", true);
                switch (_MissileStatus)
                {
                    case MissileStatusEnum.Idle:
                        {
                            _MissileStatus = MissileStatusEnum.Deploying;
                            ConfigureMissile();
                            return;
                        }
                    case MissileStatusEnum.Deploying:
                        {
                            ConfigureMissile();
                            if (!_proximitySensor.IsActive)
                            {
                                _MissileStatus = MissileStatusEnum.Approaching;
                                ConfigureMissile();
                            }
                            return;
                        }
                    case MissileStatusEnum.Approaching:
                        {
                            _MissileStatus = MissileStatusEnum.Engaging; //Bypass
                            ConfigureMissile();
                            if (IsActive(_MissileEventDistance))
                            {
                                _MissileStatus = MissileStatusEnum.Engaging;
                                ConfigureMissile();
                            }
                            return;
                        }
                    case MissileStatusEnum.Engaging:
                        {
                            ConfigureMissile();
                            return;
                        }
                    default: return;

                }
            }
        default: { Echo("Command not recognized"); return; }
    }
}

bool IsActive(IMyEventControllerBlock block)
{
    try
    {
        int distance = int.Parse(block.DetailedInfo.Split('\n').First(x => x.Contains("OffensiveCombatBlock")).Split(' ')[5]);
        _lcd?.WriteText($"Threshold: {block.Threshold}\nDistance to target: {distance}", true);
        if (block.Threshold > distance) return true;
        return false;
    }
    catch { Echo("Failed to parse distance to target!"); }
    return false;
}

bool ShouldDetonate()
{
    return
        (_MissileStatus != MissileStatusEnum.Idle) && (_missileBattery.CurrentStoredPower > 0.1f) ||
        (_MissileStatus != MissileStatusEnum.Idle) && (_missileFuelTank.FilledRatio < 0.1) ||
        (_MissileStatus == MissileStatusEnum.Engaging && _proximitySensor.IsActive) ||
        (_MissileStatus == MissileStatusEnum.Engaging && IsActive(_MissileEventDistance));
}

public void Save() { }


MissileCommandEnum MissileCommandStorage
{
    get
    {
        try { return (MissileCommandEnum)Enum.Parse(typeof(MissileCommandEnum), Storage.Split('|')[0]); }
        catch { Echo("Failed to parse command!"); return MissileCommandEnum.Idle; }
    }
    set { WriteToStorage(value.ToString(), 0); }
}


MissileStatusEnum MissileStatusStorage
{
    get
    {
        try { return (MissileStatusEnum)Enum.Parse(typeof(MissileStatusEnum), Storage.Split('|')[1]); }
        catch { Echo("Failed to parse missile status!"); return MissileStatusEnum.Idle; }
    }
    set { WriteToStorage(value.ToString(), 1); }
}

string GetMissileCommandStorageString() => MissileCommandStorage.ToString();
void SetMissileCommandStorageString(string data) => WriteToStorage(data, 0);

string GetMissileStatusStorageString() => MissileStatusStorage.ToString();
void SetMissileStatusStorageString(string data) => WriteToStorage(data, 1);

bool MissileFlightCheck
{
    get { return bool.Parse(Storage.Split('|')[2]); }
    set { WriteToStorage(value.ToString(), 2); }
}

void WriteToStorage(string data, int index)
{
    string[] hold = Storage.Split('|');
    hold[index] = data.ToString();
    Storage = "";
    foreach (string item in hold)
        Storage += item + '|';
    Storage.Remove(Storage.Length - 1);
}

public void Main(string argument, UpdateType updateSource)
{
    if (string.IsNullOrEmpty(argument) && (string.IsNullOrEmpty(Storage))) return;
    else if (string.IsNullOrEmpty(argument) && !string.IsNullOrEmpty(Storage)) argument = MissileCommandStorage.ToString();
    else if (!string.IsNullOrEmpty(argument))
    {
        if (string.IsNullOrEmpty(Storage))
            Storage = $"{argument}|Idle|{MissileFlightCheck}";
        else
            SetMissileCommandStorageString(argument);
    }
    Save();

    _lcd?.WriteText(Fliper().ToString());
    MissileStatusEnum status;
    status = MissileStatusStorage;
    MissileCommandEnum command;
    command = MissileCommandStorage;

    Operate(command);
}

byte fliperIndex = 0;
char[] positions = { '|', '/', '-', '\\' };
char Fliper()
{
    if (fliperIndex > positions.Length - 1) { fliperIndex = 0; }
    return positions[fliperIndex++];
}

public enum MissileStatusEnum
{
    Idle, Deploying, Approaching, Engaging
}
public enum MissileCommandEnum
{
    Idle, Engage
}