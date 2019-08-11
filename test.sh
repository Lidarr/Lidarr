#! /bin/bash
PLATFORM=$1
TYPE=$2
COVERAGE=$3
WHERE="cat != ManualTest"
TEST_DIR="."
TEST_PATTERN="*Test.dll"
ASSEMBLIES=""
TEST_LOG_FILE="TestLog.txt"
COVERAGE_FILE="$TEST_DIR/Coverage.xml"

if [ -d "$TEST_DIR/_tests" ]; then
  TEST_DIR="$TEST_DIR/_tests"
fi

rm -f "$TEST_LOG_FILE"

# Uncomment to log test output to a file instead of the console
export LIDARR_TESTS_LOG_OUTPUT="File"

NUNIT="$TEST_DIR/NUnit.ConsoleRunner.3.7.0/tools/nunit3-console.exe"
OPEN_COVER="$TEST_DIR/OpenCover.4.7.922/tools/OpenCover.Console.exe"
NUNIT_COMMAND="$NUNIT"
OC_COMMAND="$OPEN_COVER"
NUNIT_PARAMS="--workers=1"

if [ "$PLATFORM" = "Mac" ]; then
  LD_LIBRARY_PATH=/usr/bin:$LD_LIBRARY_PATH
  sqlite3 -version
  sqlite3 :memory: "pragma compile_options"
fi

if [ "$PLATFORM" = "Windows" ]; then
  mkdir -p "$ProgramData/Lidarr"
  WHERE="$WHERE && cat != LINUX"
elif [ "$PLATFORM" = "Linux" ] || [ "$PLATFORM" = "Mac" ] ; then
  mkdir -p ~/.config/Lidarr
  WHERE="$WHERE && cat != WINDOWS"
  NUNIT_COMMAND="mono --debug --runtime=v4.0 $NUNIT"
  OC_COMMAND="mono --debug --runtime=v4.0 $OPEN_COVER"
else
  echo "Platform must be provided as first arguement: Windows, Linux or Mac"
  exit 1
fi

if [ "$TYPE" = "Unit" ]; then
  WHERE="$WHERE && cat != IntegrationTest && cat != AutomationTest"
elif [ "$TYPE" = "Integration" ] || [ "$TYPE" = "int" ] ; then
  WHERE="$WHERE && cat == IntegrationTest"
elif [ "$TYPE" = "Automation" ] ; then
  WHERE="$WHERE && cat == AutomationTest"
else
  echo "Type must be provided as second argument: Unit, Integration or Automation"
  exit 2
fi

for i in `find $TEST_DIR -name "$TEST_PATTERN"`;
  do ASSEMBLIES="$ASSEMBLIES $i"
done

if [ "$COVERAGE" = "Coverage" ]; then
  $OC_COMMAND -register:user -target:"$NUNIT" -targetargs:"$NUNIT_PARAMS --where=\"$WHERE\" $ASSEMBLIES" -output:"$COVERAGE_FILE";
  EXIT_CODE=$?
elif [ "$COVERAGE" = "Test" ] ; then
  $NUNIT_COMMAND --where "$WHERE" $NUNIT_PARAMS $ASSEMBLIES;
  EXIT_CODE=$?
else
  echo "Run Type must be provided as third argument: Coverage or Test"
  exit 3
fi

if [ "$EXIT_CODE" -ge 0 ]; then
  echo "Failed tests: $EXIT_CODE"
  exit 0
else
  exit $EXIT_CODE
fi
