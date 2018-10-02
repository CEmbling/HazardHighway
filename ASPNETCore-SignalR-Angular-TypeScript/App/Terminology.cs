using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class Terminology
    {

        private readonly List<string> _crashVerbs = new List<string>()
        {
            "impacted","smashed","demolished","wrecked","rammed","pealed","cracked","burst",
            "dented","blasted","clocked","speared","t-boned","sliced","split","side-swiped",
            "ended","ruined","knocked","struck","thumped","whacked","slammed","bumped",
            "hammered","pounded","pummeled","discharged","blew-up","detonated"
        };
        private readonly List<string> _savedVerbs = new List<string>()
        {
            "owned","wrangled","slapped","disciplined", "tamed"
        };
        private readonly List<string> _safeAdjectives = new List<string>()
        {
            "studious","attentive","disciplined", "alert", "awake", "responsible"
        };
        private readonly List<string> _unsafeAdjectives = new List<string>()
        {
            "careless","sleepy","hazy", "distracted", "inebriated", "foggy", "moody", "dangerous", "wreckless", "mean", "buzzed"
        };

        public string GetRandomTerm(TermList termList)
        {
            Random r = new Random();
            int index = 0;
            string randomString = "";
            switch (termList)
            {
                case TermList.Tamed:
                    index = r.Next(_savedVerbs.Count);
                    randomString = _savedVerbs[index];
                    break;
                case TermList.Crash:
                    index = r.Next(_crashVerbs.Count);
                    randomString = _crashVerbs[index];
                    break;
                case TermList.Safe:
                    index = r.Next(_safeAdjectives.Count);
                    randomString = _safeAdjectives[index];
                    break;
                case TermList.Unsafe:
                    index = r.Next(_unsafeAdjectives.Count);
                    randomString = _unsafeAdjectives[index];
                    break;
            }

            return randomString;
        }
    }
}
