﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KIS
{
    static public class KIS_Shared
    {
        public static bool debugLog = true;
        public static string bipWrongSndPath = "KIS/Sounds/bipwrong";

        public static void DebugLog(string text)
        {
            if (debugLog) Debug.Log("[KIS] " + text);
        }

        public static void DebugLog(string text, UnityEngine.Object context)
        {
            if (debugLog) Debug.Log("[KIS] " + text, context);
        }

        public static void DebugWarning(string text)
        {
            if (debugLog)
            {
                Debug.LogWarning("[KIS] " + text);
            }
        }

        public static void DebugError(string text)
        {
            if (debugLog)
            {
                Debug.LogError("[KIS] " + text);
            }
        }

        public static bool createFXSound(Part part, FXGroup group, string sndPath, bool loop, float maxDistance = 30f)
        {
            group.audio = part.gameObject.AddComponent<AudioSource>();
            group.audio.volume = GameSettings.SHIP_VOLUME;
            group.audio.rolloffMode = AudioRolloffMode.Linear;
            group.audio.dopplerLevel = 0f;
            group.audio.panLevel = 1f;
            group.audio.maxDistance = maxDistance;
            group.audio.loop = loop;
            group.audio.playOnAwake = false;
            if (GameDatabase.Instance.ExistsAudioClip(sndPath))
            {
                group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
                return true;
            }
            else
            {
                KIS_Shared.DebugError("Sound not found in the game database !");
                ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your KAS installation !", 10, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
        }

        public static void DecoupleFromAll(Part p)
        {
            if (p.parent)
            {
                p.decouple();
            }
            if (p.children.Count != 0)
            {
                DecoupleAllChilds(p);
            }
        }

        public static void DecoupleAllChilds(Part p)
        {
            List<Part> partList = new List<Part>();
            foreach (Part pc in p.children)
            {
                partList.Add(pc);
            }
            foreach (Part pc2 in partList)
            {
                if (pc2.parent) pc2.decouple();
            }
        }

        public static ConfigNode PartSnapshot(Part part)
        {
            // Seems fine with a null vessel in 0.23 if some empty lists are allocated below
            ProtoPartSnapshot snapshot = new ProtoPartSnapshot(part, null);

            ConfigNode node = new ConfigNode("PART");

            snapshot.attachNodes = new List<AttachNodeSnapshot>();
            snapshot.srfAttachNode = new AttachNodeSnapshot("attach,-1");
            snapshot.symLinks = new List<ProtoPartSnapshot>();
            snapshot.symLinkIdxs = new List<int>();

            snapshot.Save(node);

            //node.AddValue("kas_total_mass", part.mass + part.GetResourceMass());

            // Prune unimportant data
            node.RemoveValues("parent");
            node.RemoveValues("position");
            node.RemoveValues("rotation");
            node.RemoveValues("istg");
            node.RemoveValues("dstg");
            node.RemoveValues("sqor");
            node.RemoveValues("sidx");
            node.RemoveValues("attm");
            node.RemoveValues("srfN");
            node.RemoveValues("attN");
            node.RemoveValues("connected");
            node.RemoveValues("attached");
            node.RemoveValues("flag");

            node.RemoveNodes("ACTIONS");

            // Remove modules that are not in prefab since they won't load anyway
            var module_nodes = node.GetNodes("MODULE");
            var prefab_modules = part.partInfo.partPrefab.GetComponents<PartModule>();
            node.RemoveNodes("MODULE");

            for (int i = 0; i < prefab_modules.Length && i < module_nodes.Length; i++)
            {
                var module = module_nodes[i];
                var name = module.GetValue("name") ?? "";

                node.AddNode(module);

                if (name == "KASModuleContainer")
                {
                    // Containers get to keep their contents
                    module.RemoveNodes("EVENTS");
                }
                else if (name.StartsWith("KASModule"))
                {
                    // Prune the state of the KAS modules completely
                    module.ClearData();
                    module.AddValue("name", name);
                    continue;
                }

                module.RemoveNodes("ACTIONS");
            }

            return node;
        }

        public static ConfigNode vesselSnapshot(Vessel vessel)
        {
            ProtoVessel snapshot = new ProtoVessel(vessel);
            ConfigNode node = new ConfigNode("VESSEL");
            snapshot.Save(node);
            return node;
        }

        public static Collider GetEvaCollider(Vessel evaVessel, string colliderName)
        {
            KerbalEVA kerbalEva = evaVessel.rootPart.gameObject.GetComponent<KerbalEVA>();
            Collider evaCollider = null;
            if (kerbalEva)
            {
                foreach (Collider col in kerbalEva.characterColliders)
                {
                    if (col.name == colliderName)
                    {
                        evaCollider = col;
                        break;
                    }
                }
            }
            return evaCollider;
        }
        /* TEST
        public static Part CreatePart(ConfigNode partConfig, Vector3 position, Quaternion rotation, Part fromPart, bool removeMe = true)
        {


    ConfigNode[] partNodes = new ConfigNode[1];


    partNodes[0] = partConfig;

            ConfigNode protoVessNode = ProtoVessel.CreateVesselNode("test",VesselType.Unknown, fromPart.orbit, 0, partNodes);

            ProtoVessel protoVess = HighLogic.CurrentGame.AddVessel(protoVessNode);
            protoVess.landed = true;

            protoVess.vesselRef.rootPart.transform.position = position;
            protoVess.vesselRef.rootPart.transform.rotation = rotation;

            return protoVess.vesselRef.rootPart;
            
            // Create part
            ConfigNode node_copy = new ConfigNode();
            partConfig.CopyTo(node_copy);
            ProtoPartSnapshot snapshot = new ProtoPartSnapshot(node_copy, null, HighLogic.CurrentGame);

            if (HighLogic.CurrentGame.flightState.ContainsFlightID(snapshot.flightID))
                snapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

            snapshot.parentIdx = 0;
            snapshot.position = position;
            snapshot.rotation = rotation;
            snapshot.stageIndex = 0;
            snapshot.defaultInverseStage = 0;
            snapshot.seqOverride = -1;
            snapshot.inStageIndex = -1;
            snapshot.attachMode = (int)AttachModes.SRF_ATTACH;
            snapshot.attached = true;
            snapshot.connected = true;
            snapshot.flagURL = fromPart.flagURL;

            Part newPart = snapshot.Load(fromPart.vessel, false);

            // Request initialization as nonphysical to prevent explosions and velocity reset at high velocity (ex : orbiting moon)
            newPart.physicalSignificance = Part.PhysicalSignificance.NONE;

            ShipConstruct newShip = new ShipConstruct();
            newShip.Add(newPart);
            newShip.SaveShip();
            newShip.shipName = newPart.partInfo.title;
            //newShip.ty = 1;

            VesselCrewManifest vessCrewManifest = new VesselCrewManifest();
            Vessel currentVessel = FlightGlobals.ActiveVessel;

            Vessel v = newShip.parts[0].localRoot.gameObject.AddComponent<Vessel>();
            v.id = Guid.NewGuid();
            v.vesselName = newShip.shipName;
            v.Initialize(false);
            v.Landed = true;
            v.rootPart.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            v.rootPart.missionID = fromPart.missionID;
            v.rootPart.flagURL = fromPart.flagURL;

            //v.rootPart.collider.isTrigger = true;

            //v.landedAt = "somewhere";

            Staging.beginFlight();
            newShip.parts[0].vessel.ResumeStaging();
            Staging.GenerateStagingSequence(newShip.parts[0].localRoot);
            Staging.RecalculateVesselStaging(newShip.parts[0].vessel);

            FlightGlobals.SetActiveVessel(currentVessel);

            v.SetPosition(position);
            v.SetRotation(rotation);

            // Solar panels from containers don't work otherwise
            for (int i = 0; i < newPart.Modules.Count; i++)
            {
                ConfigNode node = new ConfigNode();
                node.AddValue("name", newPart.Modules[i].moduleName);
                newPart.LoadModule(node, ref i);
            }

            return newPart;
        }*/

        
        public static Part CreatePart(AvailablePart avPart, Vector3 position, Quaternion rotation, Part fromPart, bool decouple = true)
        {
            ConfigNode partNode = new ConfigNode();
            PartSnapshot(avPart.partPrefab).CopyTo(partNode);
            //return CreatePart(partNode, position, rotation, fromPart, decouple);
            return CreatePart(partNode, position, rotation, fromPart);
        }
        
        public static Part CreatePart(ConfigNode partConfig, Vector3 position, Quaternion rotation, Part fromPart, bool decouple = true)
        {
            // Create and add part to a vessel and decouple it
            ConfigNode node_copy = new ConfigNode();
            partConfig.CopyTo(node_copy);
            ProtoPartSnapshot snapshot = new ProtoPartSnapshot(node_copy, null, HighLogic.CurrentGame);

            if (HighLogic.CurrentGame.flightState.ContainsFlightID(snapshot.flightID) || snapshot.flightID == 0)
            {
                snapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            }
            snapshot.parentIdx = 0;
            snapshot.position = position;
            snapshot.rotation = rotation;
            snapshot.stageIndex = 0;
            snapshot.defaultInverseStage = 0;
            snapshot.seqOverride = -1;
            snapshot.inStageIndex = -1;
            snapshot.attachMode = (int)AttachModes.SRF_ATTACH;
            snapshot.attached = true;
            snapshot.connected = true;
            snapshot.flagURL = fromPart.flagURL;

            Part newPart = snapshot.Load(fromPart.vessel, false);

            newPart.transform.position = position;
            newPart.transform.rotation = rotation;
            newPart.missionID = fromPart.missionID;

            fromPart.vessel.Parts.Add(newPart);

            // Request initialization as nonphysical to prevent explosions and velocity reset at high velocity (ex : orbiting moon)
            newPart.physicalSignificance = Part.PhysicalSignificance.NONE;

            newPart.StartCoroutine(WaitAndUnpack(newPart, decouple, fromPart));

            return newPart;
        }

        private static IEnumerator<YieldInstruction> WaitAndUnpack(Part part, bool decouple, Part fromPart)
        {
            while (!part.started && part.State != PartStates.DEAD)
            {
                yield return null;
            }

            if (part.vessel && part.State != PartStates.DEAD)
            {
                //FinishDelayedCreation(part, re_enable);
                part.PromoteToPhysicalPart();
                if (part.packed && !part.vessel.packed)
                {
                    KIS_Shared.DebugLog("WaitAndUnpack - Part is packed");
                    part.Unpack();
                    part.InitializeModules();
                    part.ResumeVelocity();
                }
          
                GameEvents.onVesselWasModified.Fire(part.vessel);
                if (decouple)
                {
                    part.decouple();
                }
                part.rigidbody.velocity = fromPart.rigidbody.velocity;
                part.rigidbody.angularVelocity = fromPart.rigidbody.angularVelocity;
                part.vessel.vesselType = VesselType.Unknown;
                GameEvents.onVesselWasModified.Fire(part.vessel);
            }
        }
   
        public static void MoveAlign(Transform source, Transform childNode, RaycastHit hit, Quaternion adjust)
        {
            Vector3 refDirection = Vector3.up;
            Vector3 alterDirection = Vector3.forward;

            Vector3 refDir = hit.transform.TransformDirection(refDirection);
            Vector3 alterDir = hit.transform.TransformDirection(alterDirection);
            Quaternion rotation;

            if (hit.normal == refDir)
            {
                rotation = Quaternion.LookRotation(hit.normal, alterDir);
            }
            else if (hit.normal == -refDir)
            {
                rotation = Quaternion.LookRotation(hit.normal, -alterDir);
            }
            else
            {
                rotation = Quaternion.LookRotation(hit.normal, refDir);
            }

            MoveAlign(source, childNode, hit.point, rotation * adjust);
        }

        public static void MoveAlign(Transform source, Transform childNode, Transform target)
        {
            MoveAlign(source, childNode, target.position, target.rotation);
        }

        public static void MoveAlign(Transform source, Transform childNode, Vector3 targetPos, Quaternion targetRot)
        {
            source.rotation = targetRot * Quaternion.Inverse(childNode.localRotation);
            source.position = source.position - (childNode.position - targetPos);
        }

        public static void ResetCollisionEnhancer(Part p, bool create_new = true)
        {
            if (p.collisionEnhancer)
            {
                UnityEngine.Object.DestroyImmediate(p.collisionEnhancer);
            }

            if (create_new)
            {
                p.collisionEnhancer = p.gameObject.AddComponent<CollisionEnhancer>();
            }
        }

        public static float GetPartVolume(Part partPrefab)
        {
            Bounds[] rendererBounds = PartGeometryUtil.GetRendererBounds(partPrefab);
            Vector3 boundsSize = PartGeometryUtil.MergeBounds(rendererBounds, partPrefab.transform).size;
            float volume = boundsSize.x * boundsSize.y * boundsSize.z;           
            return volume;
        }

        public static string GetVolumeText(float volume, bool showUnit = true)
        {
            if (volume >= 1)
            {
                string text = volume.ToString("0.00");
                if (showUnit) text += " m3";
                return text;
            }
            else
            {
                string text = (volume * 100).ToString("0.00");
                if (showUnit) text += " cm3";
                return text;
            }
        }

        public static ConfigNode GetBaseConfigNode(PartModule partModule)
        {
            UrlDir.UrlConfig pConfig = null;
            foreach (UrlDir.UrlConfig uc in GameDatabase.Instance.GetConfigs("PART"))
            {
                if (uc.name.Replace('_', '.') == partModule.part.partInfo.name)
                {
                    pConfig = uc;
                    break;
                }
            }
            if (pConfig != null)
            {
                foreach (ConfigNode cn in pConfig.config.GetNodes("MODULE"))
                {
                    if (cn.GetValue("name") == partModule.moduleName)
                    {
                        return cn;
                    }
                }
            }
            return null;
        }

        public static void AddNodeTransform(Part p, AttachNode attachNode)
        {
            Quaternion rotation = Quaternion.LookRotation(attachNode.orientation, Vector3.up);

            if (attachNode.nodeType == AttachNode.NodeType.Surface)
            {
                rotation = Quaternion.Inverse(rotation);
            }

            if (attachNode.nodeTransform == null)
            {
                Transform nodeTransform = new GameObject("KASNodeTransf").transform;
                nodeTransform.parent = p.transform;
                nodeTransform.localPosition = attachNode.position;
                nodeTransform.localRotation = rotation;
                attachNode.nodeTransform = nodeTransform;
            }
            else
            {
                attachNode.nodeTransform.localPosition = attachNode.position;
                attachNode.nodeTransform.localRotation = rotation;
                KIS_Shared.DebugLog("AddTransformToAttachNode - Node : " + attachNode.id + " already have a nodeTransform, only update");
            }
        }

        public static void EditField(string label, ref bool value, int maxLenght = 50)
        {
            value = GUILayout.Toggle(value, label);
        }

        public static Dictionary<string, string> editFields = new Dictionary<string, string>();

        public static bool EditField(string label, ref Vector3 value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label + "x")) editFields.Add(label + "x", value.x.ToString());
            if (!editFields.ContainsKey(label + "y")) editFields.Add(label + "y", value.y.ToString());
            if (!editFields.ContainsKey(label + "z")) editFields.Add(label + "z", value.z.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label + "x"] = GUILayout.TextField(editFields[label + "x"], maxLenght);
            editFields[label + "y"] = GUILayout.TextField(editFields[label + "y"], maxLenght);
            editFields[label + "z"] = GUILayout.TextField(editFields[label + "z"], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set vector"), GUILayout.Width(60f)))
            {
                Vector3 tmpVector3 = new Vector3(float.Parse(editFields[label + "x"]), float.Parse(editFields[label + "y"]), float.Parse(editFields[label + "z"]));
                value = tmpVector3;
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref string value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set string"), GUILayout.Width(60f)))
            {
                value = editFields[label];
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref int value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set int"), GUILayout.Width(60f)))
            {
                value = int.Parse(editFields[label]);
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref float value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set float"), GUILayout.Width(60f)))
            {
                value = float.Parse(editFields[label]);
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

    }
}