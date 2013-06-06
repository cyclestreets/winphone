<?php
ini_set('display_errors','On');

require_once('./functions/rwd_constants.php');
require_once 'XML/Serializer.php';

// Create our superglobal wotsit so we can save doing the same things over and over
class wotsit
{
    var $path = "";
    var $url = "";
    var $error_log  = "";
}

require_once(ROOT_PATH.'/functions/liveScoreFunctions.php');
require_once(ROOT_PATH."/engine/mysql.php");

$rwdInfo = new wotsit();

// Load config
require_once("functions/globalvars.php");

// Global functions
$std    = new liveScoreFunctions();
// Get data from global arrays
$IN     = $std->saveGlobals();
// Load the database
$dbinfo = array("sqlhost" => $CONFIG["sqlhost"],
        "sqlusername" => $CONFIG["sqlusername"],
        "sqlpassword" => $CONFIG["sqlpassword"],
        "sqldatabase" => $CONFIG["sqldatabase"],
        "sql_tbl_prefix" => $CONFIG["sqlprefix"]);

$DB = new mysql($dbinfo);

if ( $rwdInfo->error_log != null )
{
    echo "Error log: ".$rwdInfo->error_log;
}
else
{
    $udid = $IN['Hardware'];
    $DB->query("SELECT * FROM `trials` WHERE `UID`='${udid}'");
    $myrow = $DB->fetch_row();
    $time = time();
    if ( $myrow == null )
    {
        $insert = array('UID'=>$udid,
                        'time'=>$time);
        $DB->insert($insert, "trials");
    }
    else
    {
        $time=$myrow['time'];
    }
    
    $elapsed = time() - $time;
    $result = true;
    if ( $elapsed > 60 * 60 * 24 )
    {
        $result = false;
    }
    $data['result'] = $result;
    
    // An array of serializer options   
    $serializer_options = array (   
        'addDecl' => TRUE,   
        'encoding' => 'ISO-8859-1',   
        'indent' => '  ',   
        'rootName' => 'root',   
        'defaultTagName' => "trial",   
    );   
       
    // Instantiate the serializer with the options   
    $Serializer = new XML_Serializer($serializer_options);   
       
    // Serialize the data structure   
    $status = $Serializer->serialize($data);   
       
    // Check whether serialization worked   
    if (PEAR::isError($status)) 
    {   
        die($status->getMessage());   
    }   
       
    // Display the XML document   
    header('Content-type: text/xml');   
    print $Serializer->getSerializedData();      
}
?>
