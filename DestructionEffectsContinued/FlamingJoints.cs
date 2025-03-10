using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using CompoundParts;
using UnityEngine;

namespace DestructionEffects
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlamingJoints : MonoBehaviour
    {
        private const string NewFlameModelPath = "DestructionEffectsContinued/Models/FlameEffect2/model";
        private const string LegacyFlameModelPath = "DestructionEffectsContinued/Models/FlameEffect_Legacy/model";
        //private float timeNoFlames;
        //private Vessel LastVesselLoaded = null;
        public static List<GameObject> FlameObjects = new List<GameObject>();             
        public List<Vessel> vesselsAllowed = new List<Vessel>();
        // added dictionary to remember the parents of parts that are destroyed
        public Dictionary<uint, uint> deadPartsParents = new Dictionary<uint, uint>();
        private static readonly string[] PartTypesTriggeringUnwantedJointBreakEvents = new string[]
        {
            "decoupler",
            "separator",
            "docking",
            "grappling",
            "landingleg",
            "clamp",
            "gear",
            "wheel",
            "mast",
            "heatshield",
            "turret",
            "missilelauncher",
            "moudleturret",
            "missileturret",
            "missilefire",
            "kas.",
            "kis.",
            "cport,",
            "torpedo",
            "slw",
            "mortar",
            "hedg"
        };

        private static readonly string[] _PartTypesTriggeringUnwantedJointBreakEvents = new string[DESettings.PartIgnoreList.Length + PartTypesTriggeringUnwantedJointBreakEvents.Length];

        //1553 void OnPartJointBreak(PartJoint j, float breakForce)
        public void Start()
        {
            GameEvents.onPhysicsEaseStop.Add(OnPhysicsEaseStop);
            GameEvents.onPartJointBreak.Add(OnPartJointBreak);
            GameEvents.onPartWillDie.Add(OnPartWillDie);
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelLoaded);
            PartTypesTriggeringUnwantedJointBreakEvents.CopyTo(_PartTypesTriggeringUnwantedJointBreakEvents,0);
            DESettings.PartIgnoreList.CopyTo(_PartTypesTriggeringUnwantedJointBreakEvents, PartTypesTriggeringUnwantedJointBreakEvents.Length);
        }

        // this function was added as during this event the joints are still intact and we can remember the parent of the part that is going to die
        public void OnPartWillDie(Part data)
        {
            if (!(data.localRoot == data))
            {
                if (!deadPartsParents.ContainsKey(data.flightID))
                {
                    deadPartsParents.Add(data.flightID, data.parent.flightID);
                }
              
            }
            else
            {
                if (!deadPartsParents.ContainsKey(data.flightID))
                {
                    deadPartsParents.Add(data.flightID, data.flightID);
                }
            }
        }

        // this function was added to clear the list of dead parts when a scene is loaded
        public void OnLevelLoaded(GameScenes data)
        {
            deadPartsParents.Clear();
        }

        public void OnPhysicsEaseStop(Vessel data)
        {
            vesselsAllowed.Add(data);
        }

        public void OnPartJointBreak(PartJoint partJoint, float breakForce)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                return;
            }
            if (partJoint.Target == null)
            {
                return;
            }
            if (partJoint.Target.PhysicsSignificance == 1)
            {
                return;
            }

            if (vesselsAllowed.Count == 0)
            {
                return;
            }
            if (!vesselsAllowed.Contains(partJoint.Target.vessel))
            {
                return;
            }
            if (!ShouldFlamesBeAttached(partJoint))
            {
                return;
            }

            if (breakForce == 0)
            {
                // this checks if the joint connects a parent and a child (other cases are autostruts that we do not want)
                if (!(deadPartsParents.Contains(new KeyValuePair<uint, uint>(partJoint.Host.flightID, partJoint.Target.flightID)) || deadPartsParents.ContainsKey(partJoint.Target.flightID)))
                {
                    return;
                }
            }

            // added this check because if a part dies that has a still intact child there will be 2 partjoint breaks one where the target that is destroyed and one where the host is destroyed
            // also expanded attach flames with this new parameter (basically we avoid attaching flames to a destroyed object that could lead to exceptions
            bool attachToHost = false;

            if (deadPartsParents.ContainsKey(partJoint.Target.flightID))
            {
                attachToHost = true;
            }

            AttachFlames(partJoint, attachToHost);
        }

        private static void AttachFlames(PartJoint partJoint, bool attachToHost)
        {
            var modelUrl = DESettings.LegacyEffect ? LegacyFlameModelPath : NewFlameModelPath;

            // adjusted the function with this new varaible that is either the hos or the target part depending on parameters (done to avoid attaching flames to a destroyed part)
            Part flamingpart = partJoint.Target;

            if (attachToHost)
            {
                flamingpart = partJoint.Host;
            }

            var flameObject =
                (GameObject)
                    Instantiate(
                        GameDatabase.Instance.GetModel(modelUrl),
                        partJoint.transform.position,
                        Quaternion.identity);

            flameObject.SetActive(true);
            flameObject.transform.parent = flamingpart.transform;
            flameObject.AddComponent<FlamingJointScript>();

            foreach (var pe in flameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                if (!pe.useWorldSpace) continue;

                var gpe = pe.gameObject.AddComponent<DeGaplessParticleEmitter>();
                gpe.Part = flamingpart;
                gpe.Emit = true;
            }
        }

        private static bool ShouldFlamesBeAttached(PartJoint partJoint)
        {
            if (partJoint == null) return false;
            if (partJoint.Host == null) return false;
            if (!partJoint.Host) return false;
            if (partJoint.Host.Modules == null)return false;

            if (partJoint.Target == null) return false;
            if (partJoint.Target.Modules == null) return false;
            if (!partJoint.Target) return false;

            if (partJoint.Child == null) return false;
            if (partJoint.Child.Modules == null) return false;
            if (!partJoint.Child) return false;


            if (partJoint.joints.All(x => x == null)) return false;


            if (partJoint.Parent != null && partJoint.Parent.vessel != null)
            {
                if (partJoint.Parent.vessel.atmDensity <= 0.1)
                {
                    return false;
                }
            }

            var part = partJoint.Target;//SM edit for DE on ships and ship parts, adding bow, hull, stern, superstructure
       
            if (partJoint.Target.FindModulesImplementing<ModuleDecouple>().Count > 0)
            {
                return false;
            }
            
            if (partJoint.Target.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Host.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Child.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Parent?.FindModulesImplementing<CModuleStrut>().Count > 0)
            {
                return false;
            }

            if (partJoint.Target.Modules.Contains("ModuleTurret"))
            {
                return false;
            }
;
            if (IsPartHostTypeAJointBreakerTrigger(partJoint.Host.name.ToLower()))
            {
                return false;
            }

            if (part.Resources
                .Any(resource => resource.resourceName.Contains("Fuel") ||
                                 resource.resourceName.Contains("Ox") ||
                                 resource.resourceName.Contains("Elec") ||
                                 resource.resourceName.Contains("Amm") ||
                                 resource.resourceName.Contains("Cann")))
            {
                return true;
            }

            //added to lower case as in some parts the description is not uppercase in addition added some more parts that are like fuseslage (procedural structural, engine, cone, tail, cockpit)            
            if (part.partInfo.title.ToLower().Contains("wing") ||
                part.partInfo.title.ToLower().Contains("fuselage") ||
                part.partInfo.title.ToLower().Contains("bow") ||
                part.partInfo.title.ToLower().Contains("stern") ||
                part.partInfo.title.ToLower().Contains("hull") ||
                part.partInfo.title.ToLower().Contains("superstructure") ||
                part.partInfo.title.ToLower().Contains("structural") ||
                part.partInfo.title.ToLower().Contains("engine") ||
                part.partInfo.title.ToLower().Contains("cone") ||
                part.partInfo.title.ToLower().Contains("tail") ||
                part.partInfo.title.ToLower().Contains("cockpit") ||
                part.FindModuleImplementing<ModuleEngines>() != null ||
                part.FindModuleImplementing<ModuleEnginesFX>() != null)/*|| part.partInfo.title.Contains("Turret") */
            {
                return true;
            }

          
            return false;
        }

        private static bool IsPartHostTypeAJointBreakerTrigger(string hostPartName)
        {
            return _PartTypesTriggeringUnwantedJointBreakEvents.Any(hostPartName.Contains);
        }
    }
}
