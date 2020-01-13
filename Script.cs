// --------
// Settings
// --------
// custome data module id
const string CUSTOM_DATA_ID_MODULE = "Maneuver";

const string CUSTOM_DATA_ID_HEADLIGHT = "HeadLight";
const string CUSTOM_DATA_ID_WINKER    = "Winker";
const string CUSTOM_DATA_ID_TAILLIGHT = "TailLight";

// --------
// Messages
// --------
// Error
const string ERROR_UPDATE_TYPE_INVALID = "Invalid update types.";
const string ERROR_BLOCKS_NOT_FOUND    = "Loading blocks is failure.";
const string ERROR_COCKPIT_NOT_FOUND   = "Identified Cockpit Not Found.";

// --------
// Class
// --------
Blocks       blocks;
Chassis      chassis;
ErrorHandler error;

// --------
// run interval
// --------
const double EXEC_FRAME_RESOLUTION = 30;
const double EXEC_INTERVAL_TICK = 1 / EXEC_FRAME_RESOLUTION;
double currentTime = 0;

// --------
// update interval
// --------
const int UPDATE_INTERVAL = 10;
double updateTimer = 0;

public Program()
{
    // The constructor, called only once every session and
    // always before any other method is called. Use it to
    // initialize your script. 
    //     
    // The constructor is optional and can be removed if not
    // needed.
    // 
    // It's recommended to set RuntimeInfo.UpdateFrequency 
    // here, which will allow your script to run itself without a 
    // timer block.

    updateTimer = UPDATE_INTERVAL;
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

public void Main(string argument, UpdateType updateSource)
{
    // The main entry point of the script, invoked every time
    // one of the programmable block's Run actions are invoked,
    // or the script updates itself. The updateSource argument
    // describes where the update came from.
    // 
    // The method itself is required, but the arguments above
    // can be removed if not needed.

    if ( error == null ) {
        error = new ErrorHandler(this);
    }

    checkUpdateType(updateSource);

    currentTime += Runtime.TimeSinceLastRun.TotalSeconds;
    if (currentTime < EXEC_INTERVAL_TICK) {
        return;
    }

    procedure();

    error.echo();

    currentTime = 0;
}

private void checkUpdateType(UpdateType updateSource)
{
    // check updateTypes
    if( (updateSource & ( UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 | UpdateType.Once )) == 0 ) {
        error.add(ERROR_UPDATE_TYPE_INVALID);
    }
}

/**
 * main control procedure
 */
private void procedure()
{
    updateTimer += currentTime;

    if (updateTimer < UPDATE_INTERVAL) {
        Echo($"next refresh: {UPDATE_INTERVAL - updateTimer:0}");
    } else {    
        updateTimer = 0;
        Echo("updating...");

        blocks = new Blocks(GridTerminalSystem, Me.CubeGrid, error);
    }

    if( error.isExists() ) {
        return;
    }

    if ( chassis == null ) {
        chassis = new Chassis();
    }
    chassis.setCockpit(blocks.getCockpit());

    maneuverControl();
}

/**
 * maneuvering control
 */ 
private void maneuverControl()
{   
    //double velocity = chassis.getVelocity();
    
    tailLight();
    winker();
}

/* 
 *  module: tail light controller
 */
private void tailLight()
{
    foreach ( IMyLightingBlock light in blocks.tailLightList ) {
        if ( light == null ) { continue; }
        if ( false
            || chassis.Up > 0
            || ( chassis.isHandBraked() && blocks.isUnderControl )
        ) {
            light.ApplyAction("OnOff_On");
            light.Intensity = 7.5f;
        } else {
            // if it's night, then up intensity
            if ( blocks.isHeadLightOn() ) {
                light.ApplyAction("OnOff_On");
                light.Intensity = 0.5f;
            } else {
                light.ApplyAction("OnOff_Off");
            }
        }
    }
}

/* 
 *  module: winker controller
 */
private void winker()
{
    // winker left
    foreach ( IMyLightingBlock light in blocks.rightWinkerList ) {
        if ( light == null ) { continue; }
        if ( chassis.Left > 0 ) {
            light.ApplyAction("OnOff_On");
        } else {
            light.ApplyAction("OnOff_Off");
        }
    }

    // winker right
    foreach(IMyLightingBlock light in blocks.leftWinkerList){
        if ( light == null ) { continue; }
        if ( chassis.Left < 0 ) {
            light.ApplyAction("OnOff_On");
        } else {
            light.ApplyAction("OnOff_Off");
        }
    }
}

/**
 * Tank Chassis Class 
 */
private class Chassis
{
    // primary ship controller of vessel
    public IMyShipController primaryController {get; set;}

    // translation speed value
    public float Left    {get; set;}
    public float Up      {get; set;}
    public float Forward {get; set;}

    // rolling value
    public float Pitch   {get; set;}
    public float Roll    {get; set;}
    public float Yaw     {get; set;}
    
    public void setCockpit(IMyShipController Controller)
    {
        this.primaryController = Controller;
        getControl();
    }
    
    public void getControl()
    {
       updateTranslationControl();
       updateYawPitchRoll();
    }

    public void updateTranslationControl()
    {
        Vector3 translationVector = this.primaryController.MoveIndicator;

        Left    = translationVector.GetDim(0);
        Up      = translationVector.GetDim(1);
        Forward = translationVector.GetDim(2);
    }

    public void updateYawPitchRoll()
    {
        Vector2 yawPitchVector = this.primaryController.RotationIndicator;

        Pitch = yawPitchVector.X;                     // turn Up Down
        Yaw   = yawPitchVector.Y;                     // turn Left Right
        Roll  = this.primaryController.RollIndicator; // roll
    }

    public double getVelocity()
    { 
       return this.primaryController.WorldMatrix.Forward.Dot(this.primaryController.GetShipVelocities().LinearVelocity);
    }

    public bool isHandBraked()
    {
        return this.primaryController.HandBrake;
    }
}

private class Blocks
{
    IMyGridTerminalSystem grid;
    IMyCubeGrid           cubeGrid;
    ErrorHandler          error;

    List<IMyTerminalBlock> ownGridBlocksList;

    public List<IMyShipController> cockpitList;

    public List<IMyLightingBlock> headLightList;
    public List<IMyLightingBlock> tailLightList;
    public List<IMyLightingBlock> leftWinkerList;
    public List<IMyLightingBlock> rightWinkerList;

    public bool isUnderControl = false;

    // default constructor
    public Blocks(
        IMyGridTerminalSystem grid,
        IMyCubeGrid cubeGrid,
        ErrorHandler error
    )
    {
        this.grid     = grid;
        this.cubeGrid = cubeGrid;
        this.error    = error;

        this.cockpitList     = new List<IMyShipController>();

        this.headLightList   = new List<IMyLightingBlock>();
        this.tailLightList   = new List<IMyLightingBlock>();
        this.leftWinkerList  = new List<IMyLightingBlock>();
        this.rightWinkerList = new List<IMyLightingBlock>();

        this.error.clear();

        this.updateOwenedBlocks();

        if( ! this.isOwened() ) { 
            error.add(ERROR_BLOCKS_NOT_FOUND);
            return;
        }

        this.clear();

        this.assign();
    }

    public IMyShipController getCockpit() {
        // if exists Undercontrol cockpit, then use it.
        foreach(IMyShipController controller in this.cockpitList){
            if ( controller.IsUnderControl ) {
                this.isUnderControl = true;
                return controller;
            }
        }
        this.isUnderControl = false;

        return this.cockpitList[0];
    }

    private void updateOwenedBlocks()
    {
        this.ownGridBlocksList = new List<IMyTerminalBlock>();

        grid.GetBlocksOfType<IMyMechanicalConnectionBlock>(this.ownGridBlocksList);

        HashSet<IMyCubeGrid> CubeGridSet = new HashSet<IMyCubeGrid>();
        CubeGridSet.Add(this.cubeGrid);

        bool isExists;
        IMyMechanicalConnectionBlock block;

        // get all of CubeGrid by end of it
        // this is neccessary if you put cockpit on subgrid (ex: turret)
        do{
            isExists = false;
            for( int i = 0; i < this.ownGridBlocksList.Count; i++ ) {
                block = this.ownGridBlocksList[i] as IMyMechanicalConnectionBlock;

                if ( CubeGridSet.Contains(block.CubeGrid) || CubeGridSet.Contains(block.TopGrid) ) {
                    CubeGridSet.Add(block.CubeGrid);
                    CubeGridSet.Add(block.TopGrid);
                    this.ownGridBlocksList.Remove(this.ownGridBlocksList[i]);
                    isExists = true;
                }
            }
        }
        while(isExists);

        //get filtered block
        this.ownGridBlocksList.Clear();

        grid.GetBlocksOfType<IMyTerminalBlock>(this.ownGridBlocksList, owenedBlock => CubeGridSet.Contains(owenedBlock.CubeGrid));
    }

    private bool isOwened()
    {
        if (this.ownGridBlocksList.Count > 0) {
            return true;
        }
        return false;
    }

    public bool isHeadLightOn()
    { 
        foreach(IMyLightingBlock light in this.headLightList){
            if ( light.IsWorking ) {
                return true;
            }
        }
        return false;
    }

    private void clear()
    {
        this.cockpitList.Clear();

        this.headLightList.Clear();
        this.tailLightList.Clear();
        this.leftWinkerList.Clear();
        this.rightWinkerList.Clear();
    }

    private void assign()
    {
        List<IMyLightingBlock> lightList = new List<IMyLightingBlock>();
        List<IMyMotorStator>   rotorList = new List<IMyMotorStator>();

        foreach ( IMyTerminalBlock block in this.ownGridBlocksList ) {
            if ( block is IMyShipController && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE) ) {
                this.cockpitList.Add(block as IMyShipController);
                continue;
            }
            if ( block is IMyLightingBlock && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE) ) {
                lightList.Add(block as IMyLightingBlock);
                continue;
            }
            if ( block is IMyMotorStator && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE) ) {
                rotorList.Add(block as IMyMotorStator);
                continue;
            }
        }

        if( ! this.isExistsCockpit() ) { 
            this.error.add(ERROR_COCKPIT_NOT_FOUND);
            return;
        }

        var centerOfMass = this.getCockpit().CenterOfMass;

        foreach ( IMyLightingBlock light in lightList ) {
            if ( light.CustomData.Contains(CUSTOM_DATA_ID_HEADLIGHT) ) {
                headLightList.Add(light);
                continue;
            }
            if ( light.CustomData.Contains(CUSTOM_DATA_ID_TAILLIGHT) ) {
                tailLightList.Add(light);
                continue;
            }
            if ( light.CustomData.Contains(CUSTOM_DATA_ID_WINKER) ){
                if ( this.cockpitList[0].WorldMatrix.Right.Dot(light.GetPosition() - centerOfMass) > 0 ) {
                    rightWinkerList.Add(light);
                } else {
                    leftWinkerList.Add(light);
                }
                continue;
            }
        }
    }

    private bool isExistsCockpit()
    {
        if ( this.cockpitList.Count > 0 ) {
            return true;
        }
        return false;
    }
}

private class ErrorHandler
{
    private Program program;
    private List<string> errorList = new List<string>();

    public ErrorHandler(Program program)
    {
        this.program = program;
    }

    public bool isExists()
    {
        if ( this.errorList.Count > 0 ) {
            return true;
        }
        return false;
    }

    public void add(string error)
    {
        this.errorList.Add(error);
    }

    public void clear()
    {
        this.errorList.Clear();
    }

    public void echo()
    { 
        foreach ( string error in this.errorList ) {
            this.program.Echo("Error: " + error);
        }
    }
}
