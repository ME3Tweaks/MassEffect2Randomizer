Class MERSeqAct_RandomizeProbe extends SequenceAction
    config(Game);

// Types
struct ProbeMeshInfo 
{
    var string ProbeMesh;
    var float Scale;
};

// Variables
var config array<ProbeMeshInfo> MeshOptions;
var InterpActor ProbeInterpActor;

// Functions
function Activated()
{
    local ProbeMeshInfo MeshToUse;
    local StaticMesh SM;
    
    if (ProbeInterpActor == None || MeshOptions.Length == 0)
    {
        return;
    }
    MeshToUse = MeshOptions[Rand(MeshOptions.Length)];
    ProbeInterpActor.SetDrawScale(MeshToUse.Scale);
    SM = StaticMesh(Class'SFXEngine'.static.LoadSeekFreeObject(MeshToUse.ProbeMesh, Class'StaticMesh'));
    if (SM != None)
    {
        ProbeInterpActor.StaticMeshComponent.SetStaticMesh(SM);
    }
}

//class default properties can be edited in the Properties tab for the class's Default__ object.
defaultproperties
{
    VariableLinks = ({
                      LinkedVariables = (), 
                      LinkDesc = "Probe", 
                      ExpectedType = Class'SeqVar_Object', 
                      LinkVar = 'None', 
                      PropertyName = 'ProbeInterpActor', 
                      CachedProperty = None, 
                      MinVars = 1, 
                      MaxVars = 255, 
                      bWriteable = FALSE, 
                      bModifiesLinkedObject = FALSE, 
                      bAllowAnyType = FALSE
                     }
                    )
}