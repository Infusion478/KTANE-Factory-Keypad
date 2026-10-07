using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class FactoryKeypad : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public KMSelectable[] Keys;
   public TextMesh DisplayText;
   public AudioSource PressSound;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   private bool ModuleStriking;
   private int code;
   private int activationYear = DateTime.Now.Year;
   private int strikes;
   private bool TwitchInput;

   void Awake () {
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      foreach (KMSelectable Key in Keys) {
         Key.OnInteract += delegate () { KeyPress(Key); return false; };
      }
   }

   void KeyPress (KMSelectable Key) {
      if (ModuleSolved || ModuleStriking) {
         return;
      }
      for (int i = 0; i < 12; i++) {
         if (Key == Keys[i]) {
            Keys[i].AddInteractionPunch(.5f);
            StartCoroutine(KeyPress(i));
            if (i < 10) {
               if (DisplayText.text.Length < 4) {
                  DisplayText.text += ((i + 1) % 10).ToString();
                  PressSound.pitch = 1f;
                  PressSound.Play();
               } else {
                  Audio.PlaySoundAtTransform("Nope", Key.transform);
               }
            } else if (i == 10) {
               DisplayText.text = "";
               PressSound.pitch = 2f;
               PressSound.Play();
            } else {
               CheckInput();
            }
         }
      }
      return;
   }

   void Activate () {
      DisplayText.text = "";
   }

   void Start () {
      code = activationYear;
      Debug.LogFormat("[Factory Keypad #{0}] The year of activation is {1}.", ModuleId, code);
      if (Bomb.GetBatteryCount() == 0) {
         int[] temp = {code / 1000, (code / 100) % 10, (code / 10) % 10, code % 10};
         Array.Reverse(temp);
         code = temp[0] * 1000 + temp[1] * 100 + temp[2] * 10 + temp[3];
         Debug.LogFormat("[Factory Keypad #{0}] There are no batteries, so the code is reversed to become {1}.", ModuleId, code);
      } else {
         code %= 1000;
         code += (Bomb.GetBatteryCount()) * 1000;
         if (Bomb.GetBatteryCount() == 1) {
            Debug.LogFormat("[Factory Keypad #{0}] There is 1 battery, so the code becomes {1}.", ModuleId, code);
         } else {
            Debug.LogFormat("[Factory Keypad #{0}] There are {1} batteries, so the code becomes {2}.", ModuleId, Bomb.GetBatteryCount(), code);
         }
      }
      if (Bomb.GetIndicators().Count() == 0) {
         code += 1597;
         Debug.LogFormat("[Factory Keypad #{0}] There are no indicators, so the code is now {1}.", ModuleId, code);
      } else {
         foreach (string ind in Bomb.GetIndicators()) {
            if (Bomb.IsIndicatorOn(ind)) {
               if ("ABCDEFGHIJKLM".Contains(ind.ToCharArray()[0])) {
                  code += 903;
               } else if ("AEIOUY".Contains(ind.ToCharArray()[0]) || "AEIOUY".Contains(ind.ToCharArray()[1]) || "AEIOUY".Contains(ind.ToCharArray()[2])) {
                  code += 2532;
               } else {
                  code += 1111;
               }
               Debug.LogFormat("[Factory Keypad #{0}] There is a lit {1} indicator, making the code {2}.", ModuleId, ind, code);
            } else {
               if ("NOPQRSTUVWXYZ".Contains(ind.ToCharArray()[2])) {
                  code -= 309;
               } else if (ind.ToCharArray().Contains('R')) {
                  code -= 2618;
               } else {
                  code -= 555;
               }
               Debug.LogFormat("[Factory Keypad #{0}] There is an unlit {1} indicator, making the code {2}.", ModuleId, ind, code);
            }
         }
      }
      if (code < 0) {
         code = Math.Abs(code);
         Debug.LogFormat("[Factory Keypad #{0}] This yields a negative number, so the code is now {1}.", ModuleId, code);
      }
      if (code > 9999) {
         code %= 10000;
         Debug.LogFormat("[Factory Keypad #{0}] The number has more than four digits, so it is now {1}.", ModuleId, code);
      }
      string[] ports = Bomb.GetPorts().ToArray();
      Array.Sort(ports);
      foreach (string port in ports) {
         switch (port) {
            case "DVI":
               code -= 4229;
               if (code < 0) {
                  code += 10000;
               }
               Debug.LogFormat("[Factory Keypad #{0}] A DVI-D port makes the code {1}.", ModuleId, code);
               break;
            case "Parallel":
               code += 25;
               code *= 3;
               code /= 2;
               Debug.LogFormat("[Factory Keypad #{0}] A parallel port makes the code {1}.", ModuleId, code);
               break;
            case "PS2":
               code += 2000;
               code %= 10000;
               if ((code / 100) % 10 < 8) {
                  code += 200;
               } else {
                  code -= 800;
               }
               if ((code / 10) % 10 < 8) {
                  code += 20;
               } else {
                  code -= 80;
               }
               if (code % 10 < 8) {
                  code += 2;
               } else {
                  code -= 8;
               }
               Debug.LogFormat("[Factory Keypad #{0}] A PS/2 port makes the code {1}.", ModuleId, code);
               break;
            case "RJ45":
               code += 90;
               Debug.LogFormat("[Factory Keypad #{0}] An RJ-45 port makes the code {1}.", ModuleId, code);
               break;
            case "Serial":
               code /= 3;
               Debug.LogFormat("[Factory Keypad #{0}] A serial port makes the code {1}.", ModuleId, code);
               break;
            case "StereoRCA":
               code *= 2;
               Debug.LogFormat("[Factory Keypad #{0}] A Stereo RCA port makes the code {1}.", ModuleId, code);
               break;
            default:
               Debug.LogFormat("[Factory Keypad #{0}] A modded port has been detected, or something went wrong. The code has not been modified.", ModuleId);
               break;
         }
      }
      if (code > 9999) {
         code %= 10000;
         Debug.LogFormat("[Factory Keypad #{0}] The number has more than four digits, so it is now {1}.", ModuleId, code);
      }
      code += (Bomb.GetSerialNumberNumbers().First() * 1000);
      code += Bomb.GetSerialNumberNumbers().Last();
      code %= 10000;
      Debug.LogFormat("[Factory Keypad #{0}] The first part of the serial number modification makes the code {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
      switch (Bomb.GetSerialNumberLetters().Count()) {
         case 4:
            for (int i = 0; i < 4; i++) {
               code += (Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), Bomb.GetSerialNumberLetters().ElementAt(i))) + 1;
            }
            Debug.LogFormat("[Factory Keypad #{0}] The serial number has four letters, making the code {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
            break;
         case 3:
            code *= 100;
            int j = code / 10000;
            code %= 10000;
            code += j;
            Debug.LogFormat("[Factory Keypad #{0}] The serial number has three letters, making the code {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
            break;
         case 2:
            code -= Math.Abs(Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), Bomb.GetSerialNumberLetters().First()) - Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), Bomb.GetSerialNumberLetters().Last())) * 10;
            code = Math.Abs(code);
            Debug.LogFormat("[Factory Keypad #{0}] The serial number has two letters, making the code {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
            break;
         default:
            code *= 10;
            code %= 10000;
            Debug.LogFormat("[Factory Keypad #{0}] The serial number has an abnormal number of letters, making the code {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
            break;
      }
      if (Bomb.GetSerialNumberLetters().Contains('A') && Bomb.GetSerialNumberNumbers().Contains(9)) {
         code += 219;
         Debug.LogFormat("[Factory Keypad #{0}] Please report to Room A9. The code is now {1}{2}{3}{4}.", ModuleId, code < 1000?"0":"", code < 100?"0":"", code < 10?"0":"", code);
      }
      if (code < 1000) {
         code += 9000;
      }
      Debug.LogFormat("[Factory Keypad #{0}] The final code to input into the module is {1}.", ModuleId, code);
   }

   void CheckInput () {
      if (DisplayText.text == "") {
         Audio.PlaySoundAtTransform("Nope", transform);
         Debug.LogFormat("[Factory Keypad #{0}] You can't submit nothing...", ModuleId);
         return;
      }
      if (int.Parse(DisplayText.text) == code) {
         Debug.LogFormat("[Factory Keypad #{0}] Submitted {1}, which is correct. Module unlocked!", ModuleId, DisplayText.text);
         Solve();
      } else if (DisplayText.text == "0000") {
         Debug.LogFormat("[Factory Keypad #{0}] RESET CODE 0000 ENTERED — UNAUTHORIZED ATTEMPT DETECTED — ABORTING BOMB", ModuleId);
         StartCoroutine(ResetCode());
      } else {
         Incorrect();
      }
   }

   void Incorrect () {
      strikes++;
      if (strikes < 3) {
         Debug.LogFormat("[Factory Keypad #{0}] Submitted {1}, which is incorrect. {2} attempt{3} left.", ModuleId, DisplayText.text, 3 - strikes, strikes == 2?"":"s");
         Strike();
      } else {
         Debug.LogFormat("[Factory Keypad #{0}] Submitted {1}, which is incorrect. No attempts left. Detonating bomb.", ModuleId, DisplayText.text);
         if (!TwitchInput) {
            for (int i = 0; i < 1000; i++) {
               GetComponent<KMBombModule>().HandleStrike();
            }
         }
      }
   }

   void Solve () {
      ModuleSolved = true;
      Audio.PlaySoundAtTransform("Access Granted", transform);
      DisplayText.color = Color.green;
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      ModuleStriking = true;
      Audio.PlaySoundAtTransform("Incorrect Code", transform);
      GetComponent<KMBombModule>().HandleStrike();
      StartCoroutine(StrikeAnim());
   }

   IEnumerator KeyPress(int i) {
      for (int j = 0; j < 5; j++)
      {
         Keys[i].transform.localPosition -= new Vector3(0, 0, 0.005f / 5);
         yield return null;
      }
      for (int j = 0; j < 5; j++)
      {
         Keys[i].transform.localPosition += new Vector3(0, 0, 0.005f / 5);
         yield return null;
      }
   }

   IEnumerator StrikeAnim() {
      DisplayText.color = Color.red;
      yield return new WaitForSeconds(1.5f);
      float r = 1;
      for (int i = 0; i < 10; i++) {
         r = (9 - Convert.ToSingle(i)) / 10;
         DisplayText.color = new Color(r, 0f, 0f, 1f);
         yield return new WaitForSeconds(.01f);
      }
      DisplayText.text = "";
      DisplayText.color = Color.white;
      ModuleStriking = false;
   }

   IEnumerator ResetCode() {
      ModuleStriking = true;
      Audio.PlaySoundAtTransform("Alarm", transform);
      for (int i = 0; i < 4; i++) {
         DisplayText.color = new Color(1f, .5f, 0f, 1f);
         yield return new WaitForSeconds(.45f);
         DisplayText.color = Color.red;
         yield return new WaitForSeconds(.45f);
      }
      if (!Application.isEditor) {
         Application.Quit();
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} enter 1234 to enter a code. Use !{0} clear to clear input and !{0} submit to submit the input.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToLower();
      yield return null;
      if (ModuleStriking) {
         yield return "sendtochaterror The module is not accepting input right now.";
         yield break;
      }
      if (Command == "clear") {
         Keys[10].OnInteract();
      } else if (Command == "submit") {
         TwitchInput = true;
         if (int.Parse(DisplayText.text) == code) {
            Keys[11].OnInteract();
         } else if (DisplayText.text == "0000") {
            yield return "antitroll Sorry, but the remote input service has not allowed you to submit the reset code.";
            yield return null;
            Keys[11].OnInteract();
         } else {
            if (strikes < 2) {
               Keys[11].OnInteract();
            } else {
               yield return "detonate";
            }
         }
      } else if (Regex.IsMatch(Command, @"^\s*(enter)\s+\d{4}\s*$")) {
         string[] Commands = Command.Split(' ');
         char[] Input = Commands[1].ToCharArray();
         for (int i = 0; i < 4; i++) {
            if ((Input[i]) == '0') {
               Keys[9].OnInteract();
            } else {
               Keys[int.Parse(Input[i].ToString()) - 1].OnInteract();
            }
            yield return new WaitForSeconds(.1f);
         }
      } else {
         yield return "sendtochaterror Invalid command.";
         yield break;
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      Keys[10].OnInteract();
      yield return new WaitForSeconds(.1f);
      for (int i = 0; i < 4; i++) {
         int[] answer = {code / 1000, (code / 100) % 10, (code / 10) % 10, code % 10};
         answer.ToString().ToCharArray();
         if (answer[i] == '0') {
               Keys[9].OnInteract();
            } else {
               Keys[int.Parse(answer[i].ToString()) - 1].OnInteract();
            }
         yield return new WaitForSeconds(.1f);
      }
      Keys[11].OnInteract();
   }
}
