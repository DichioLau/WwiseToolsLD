/* AUTO-GENERATED FILE — do not edit manually
 * Re-generate via Tools/AKLD menu. */
using UnityEngine;

public partial class AKLD_SOTemplate
{
	// Events — from asset: Hola

	// RTPCs — from asset: Hola

	// Switches — from asset: Hola

	// States — from asset: Hola

	// Events — from asset: SOAudioTest
	public void EventTest(GameObject emitter) => GetEventComponent("EventTest")?.Post(emitter);
	public void EventTest2(GameObject emitter) => GetEventComponent("EventTest2")?.Post(emitter);
	public void EventNumber1(GameObject emitter) => GetEventComponent("EventNumber1")?.Post(emitter);
	public void EventNumber2(GameObject emitter) => GetEventComponent("EventNumber2")?.Post(emitter);
	public void EventNumber3(GameObject emitter) => GetEventComponent("EventNumber3")?.Post(emitter);
	public void EventNumber4(GameObject emitter) => GetEventComponent("EventNumber4")?.Post(emitter);
	public void Music(GameObject emitter) => GetEventComponent("Music")?.Post(emitter);

	// RTPCs — from asset: SOAudioTest
	public void MusicValue(float value)
	{
		var _rtpc = GetRTPCComponent("MusicValue");
		if (_rtpc == null) { Debug.LogWarning("[AKLD] RTPC not found: MusicValue"); return; }
		AkSoundEngine.SetRTPCValue(_rtpc.Id, value);
	}
	public void MusicValue(GameObject _, float value) => MusicValue(value);

	// States — from asset: SOAudioTest
	public void Layer1() => GetStateComponent("Layer1")?.SetValue();
	public void Layer1(GameObject _) => GetStateComponent("Layer1")?.SetValue();
	public void Layer2() => GetStateComponent("Layer2")?.SetValue();
	public void Layer2(GameObject _) => GetStateComponent("Layer2")?.SetValue();
	public void Layer3() => GetStateComponent("Layer3")?.SetValue();
	public void Layer3(GameObject _) => GetStateComponent("Layer3")?.SetValue();
	public void Layer4() => GetStateComponent("Layer4")?.SetValue();
	public void Layer4(GameObject _) => GetStateComponent("Layer4")?.SetValue();

}
