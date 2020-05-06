using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2 {
    namespace Tests {
        public class TestManager : MonoBehaviour
        {
            public Test[] awakeTests = null;
            public Test[] startTests = null;
            public Test[] updateTests = null;
            public int awakePassed { get; private set; } = 0;
            public int startPassed { get; private set; } = 0;
            public int updatePassed { get; private set; } = 0;
            public int totalPassed
            {
                get { return awakePassed + startPassed + updatePassed; }
            }

            private int awakeTestCount = 0;
            private int startTestCount = 0;
            private int updateTestCount = 0;
            private int totalTestCount
            {
                get { return awakeTestCount + startTestCount + updateTestCount; }
            }

            private void Awake()
            {
                awakeTests = GetComponents<AwakeTest>();
                if (awakeTests != null) awakeTestCount += awakeTests.Length;

                startTests = GetComponents<StartTest>();
                if (startTests != null) startTestCount += startTests.Length;

                updateTests = GetComponents<UpdateTest>();
                if (updateTests != null) updateTestCount += updateTests.Length;

                // If we have no tests to run, don't bother
                if (totalTestCount == 0) OnQuit();

                awakePassed += RunTests(awakeTests);
            }
            // Start is called before the first frame update
            void Start()
            {
                startPassed += RunTests(startTests);
            }

            // Update is called once per frame
            void Update()
            {
                updatePassed += RunTests(updateTests);
                OnQuit();
            }
            private int RunTests(Test[] tests)
            {
                int passed = 0;
                if (tests != null && tests.Length > 0)
                {
                    for (int i = 0; i < tests.Length; i++)
                    {
                        // Run PreTest()
                        if (tests[i].PreTest())
                        {
                            // Run Test()
                            if (tests[i].RunTest())
                            {
                                passed++;
                                Debug.Log("PASSED: " + tests[i].ToString());

                                // Run PostTest()
                                if (!tests[i].PostTest()) Debug.LogError("POSTTEST FAILED: " + tests[i].ToString());
                            } // Else report failed test
                            else Debug.Log("FAILED: " + tests[i].ToString());
                        }
                        else Debug.LogError("PRETEST FAILED: " + tests[i].ToString() + ". Test not run.");
                    }
                } else return 0;         

                return passed;
            }
            private void OnQuit()
            {
                // Calculate each passed rate
                float awakePassedRate = (awakeTestCount > 0) ? (awakePassed / awakeTestCount) : 0f;
                float startPassedRate = (startTestCount > 0) ? (startPassed / startTestCount) : 0f;
                float updatePassedRate = (updateTestCount > 0) ? (updatePassed / updateTestCount) : 0f;
                float totalPassedRate = (totalTestCount > 0) ? (totalPassed / totalTestCount) : 0f;

                // Print results
                string s = "TEST RESULTS:"
                    + "\n\tAWAKE TESTS: " + 100 * awakePassedRate + " (" + awakePassed + "\\" + awakeTestCount + ")"
                    + "\n\tSTART TESTS: " + 100 * startPassedRate + " (" + startPassed + "\\" + startTestCount + ")"
                    + "\n\tUPDATE TESTS: " + 100 * startPassedRate + " (" + updatePassed + "\\" + updateTestCount + ")"
                    + "\n\t-----------------------------------------------------------------------------------------------"
                + "\n\tTOTAL: " + 100 * totalPassedRate + " (" + totalPassed + "\\" + totalTestCount + ")";
                Debug.Log(s);

                // Kill yourself
                Destroy(this);
            }
            private void OnApplicationQuit()
            {
                OnQuit();
            }
        }
    }
}