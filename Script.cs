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

    error = new ErrorHandler(this);
    blocks = new Blocks(GridTerminalSystem, Me.CubeGrid, error);
    chassis = new Chassis();
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

        blocks.init();

        chassis.setController(blocks.getController());
        chassis.HeadLights   = blocks.HeadLights;
        chassis.TailLights   = blocks.TailLights;
        chassis.LeftWinkers  = blocks.LeftWinkers;
        chassis.RightWinkers = blocks.RightWinkers;
    }

    if( error.isExists() ) {
        return;
    }
    
    chassis.update();
}

/**
 * Tank Chassis Class 
 */
private class Chassis
{
    // primary ship controller(Cockpit/RemoteControl) of vessel
    public IMyShipController primaryController {get; set;}

    public List<IMyLightingBlock> HeadLights {get; set;}
    public List<IMyLightingBlock> TailLights {get; set;}
    public List<IMyLightingBlock> LeftWinkers {get; set;}
    public List<IMyLightingBlock> RightWinkers {get; set;}

    // translation speed value
    public float Left    {get; set;}
    public float Up      {get; set;}
    public float Forward {get; set;}

    // rolling value
    public float Pitch   {get; set;}
    public float Roll    {get; set;}
    public float Yaw     {get; set;}
    
    public void setController(IMyShipController Controller)
    {
        this.primaryController = Controller;
    }

    public void update()
    {
       this.updateTranslationControl();
       this.updateYawPitchRoll();
       this.updateManeuver();
    }

    private void updateTranslationControl()
    {
        Vector3 translationVector = this.primaryController.MoveIndicator;

        this.Left    = translationVector.GetDim(0);
        this.Up      = translationVector.GetDim(1);
        this.Forward = translationVector.GetDim(2);
    }

    public void updateYawPitchRoll()
    {
        //Vector2 yawPitchVector = this.primaryController.RotationIndicator;

        //this.Pitch = yawPitchVector.X;                     // turn Up Down
        //this.Yaw   = yawPitchVector.Y;                     // turn Left Right
        //this.Roll  = this.primaryController.RollIndicator; // roll
    }

    public double getVelocity()
    { 
       return this.primaryController.WorldMatrix.Forward.Dot(this.primaryController.GetShipVelocities().LinearVelocity);
    }

    public bool isHandBraked()
    {
        return this.primaryController.HandBrake;
    }

    public bool isHeadLightOn()
    { 
        foreach(IMyLightingBlock light in this.HeadLights){
            if ( light.IsWorking ) {
                return true;
            }
        }
        return false;
    }

    /**
     * maneuvering control
     */ 
    private void updateManeuver()
    {   
        //double velocity = this.getVelocity();
    
        this.tailLight();
        this.winker();
    }

    /* 
     *  module: tail light controller
     */
    private void tailLight()
    {
        foreach ( IMyLightingBlock light in this.TailLights) {
            if ( light == null ) { continue; }
            if ( false
                || this.Up > 0
                || ( this.isHandBraked() && this.primaryController.IsUnderControl )
            ) {
                light.ApplyAction("OnOff_On");
                light.Intensity = 7.5f;
            } else {
                // if it's night, then up intensity
                if ( this.isHeadLightOn() ) {
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
        foreach ( IMyLightingBlock light in this.RightWinkers) {
            if ( light == null ) { continue; }
            if ( this.Left > 0 ) {
                light.ApplyAction("OnOff_On");
            } else {
                light.ApplyAction("OnOff_Off");
            }
        }

        // winker right
        foreach(IMyLightingBlock light in this.LeftWinkers){
            if ( light == null ) { continue; }
            if ( this.Left < 0 ) {
                light.ApplyAction("OnOff_On");
            } else {
                light.ApplyAction("OnOff_Off");
            }
        }
    }
}

private class Blocks
{
    IMyGridTerminalSystem grid;
    IMyCubeGrid           cubeGrid;
    ErrorHandler          error;

    List<IMyTerminalBlock> ownGridBlocksList;

    private IMyShipController primaryController;

    public List<IMyShipController> cockpitList= new List<IMyShipController>();

    public List<IMyLightingBlock> HeadLights {get; set;} = new List<IMyLightingBlock>();
    public List<IMyLightingBlock> TailLights {get; set;} = new List<IMyLightingBlock>();
    public List<IMyLightingBlock> LeftWinkers {get; set;} = new List<IMyLightingBlock>();
    public List<IMyLightingBlock> RightWinkers {get; set;} = new List<IMyLightingBlock>();

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

        this.init();
    }

    public void init(){
        this.error.clear();

        this.updateOwenedBlocks();

        if( ! this.isOwened() ) { 
            error.add(ERROR_BLOCKS_NOT_FOUND);
            return;
        }

        this.clear();

        this.assign();
    }

    public IMyShipController getController() {
        if (this.primaryController != null) {
            return this.primaryController;
        }

        // if exists Undercontrol cockpit, then use it.
        foreach(IMyShipController controller in this.cockpitList){
            if ( controller.IsUnderControl ) {
                this.primaryController = controller;
                this.isUnderControl = true;
                return this.primaryController;
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

    private void clear()
    {
        this.cockpitList.Clear();

        this.HeadLights.Clear();
        this.TailLights.Clear();
        this.LeftWinkers.Clear();
        this.RightWinkers.Clear();
    }

    private void assign()
    {
        List<IMyLightingBlock> lightList = new List<IMyLightingBlock>();

        foreach ( IMyTerminalBlock block in this.ownGridBlocksList ) {
            if ( block is IMyShipController && this.isModule(block) ) {
                this.cockpitList.Add(block as IMyShipController);
                continue;
            }
            if ( block is IMyLightingBlock && this.isModule(block) ) {
                lightList.Add(block as IMyLightingBlock);
                continue;
            }
        }

        if( ! this.isExistsCockpit() ) { 
            this.error.add(ERROR_COCKPIT_NOT_FOUND);
            return;
        }

        var centerOfMass = this.getController().CenterOfMass;

        foreach ( IMyLightingBlock light in lightList ) {
            if ( this.isHeadLight(light) ) {
                HeadLights.Add(light);
                continue;
            }
            if ( this.isTailLight(light) ) {
                TailLights.Add(light);
                continue;
            }
            if ( this.isWinker(light) ){
                if ( this.cockpitList[0].WorldMatrix.Right.Dot(light.GetPosition() - centerOfMass) > 0 ) {
                    RightWinkers.Add(light);
                } else {
                    LeftWinkers.Add(light);
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

    private bool isModule(IMyTerminalBlock block){
        if (block.CustomData.Contains(CUSTOM_DATA_ID_MODULE)){
            return true;
        } else {
            return false;
        } 
    }

    private bool isTailLight(IMyTerminalBlock block){
        if (block.CustomData.Contains(CUSTOM_DATA_ID_TAILLIGHT)) {
            return true;
        } else {
            return false;
        } 
    }

    private bool isHeadLight(IMyTerminalBlock block){
        if (block.CustomData.Contains(CUSTOM_DATA_ID_HEADLIGHT)) {
            return true;
        } else {
            return false;
        }         
    }

    private bool isWinker(IMyTerminalBlock block){
        if (block.CustomData.Contains(CUSTOM_DATA_ID_WINKER)) {
            return true;
        } else {
            return false;
        }  
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
