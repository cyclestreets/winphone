<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/debug.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
 
function echoArray($array)
{
	echo "<pre>";
	print_r($array);
	echo "</pre>";
	return $output;
}

function getTraceName() 
{

	// Just call getTraceName() to see the name of the function or class that called
	// the current function

	$vDebug = debug_backtrace();	
	$fname = "";
	
	if (count($vDebug) > 1) {
		$aFile = $vDebug[2]; 
		$fname = $aFile['class'] . $aFile['type'] . $aFile['function'];
	}

	return $fname;
} 

function getTrace($type=0) 
{

	// Just call getTrace() from your application to view the callstack.

	// getTrace()  = HTML output
	// getTrace(0) = HTML output
	// getTrace(1) = NO HTML

	$vDebug = debug_backtrace();
    //echo "<pre>";
    //print_r($vDebug);
    //exit();
	$lbreak = "<BR>";
	if ($type == 1) {
		$lbreak = "\n";
	}

	echo "[--------------------------------------------------------------------------------------------]$lbreak";
	echo "Call stack is " . (count($vDebug) -1) . " deep. $lbreak";

	if ($type == 0) {
		echo "<table cellpadding=10 border=1 bgcolor=lightyellow><tr>";
		echo "<td><B>Number</b></td>";
		echo "<td><B>File</b></td>";
		echo "<td><B>Line #</b></td>";
		echo "<td><B>Function/Method</b></td>";
		echo "<td><B>Argument(s)</b></td>";
		echo "</tr>";
	}

	for ($i=0;$i<count($vDebug);$i++) {

   		// skip the first one, since it's always this func
   		if ($i==0) { continue; }

		$t = count($vDebug) - $i;
		$num = sprintf("[%03d] : ", $t);
		$aFile = $vDebug[$i];

        if ( $aFile['args'] )
		    $args = implode(',',$aFile['args']);
		if (strlen($args) < 1) {
			$args = "[None]";
		}

		$f = $aFile['function'];
		$f2 = strtoupper($f);
		if (substr($f2,0,7) == "REQUIRE" || substr($f2,0,7) == "INCLUDE") {

			$args = basename($args);

		}

		
		if ($type == 1) {
            if ( !strstr($aFile['function'], "sg_load") )
            {
			    echo $num . " : " . basename($aFile['file']) . "(line " . $aFile['line'] . ") -- " . $aFile['class'] . $aFile['type'] . $aFile['function'] . "( " . $args . " ) \n";
            }
		}
		else {
			echo "<TR>";
			echo "<td>";
			printf("%03d", $t);
			echo "</td>";
			echo "<td> " . basename($aFile['file']) . "</td>";
			echo "<td> " . $aFile['line'] . "</td>";
			echo "<td> " . $aFile['class'] . $aFile['type'] . $aFile['function'] . "() </td>";




			echo "<td> " . $args . " </td>";
			echo "</tr>";
		}

        $args = "";
 	} // for

	if ($type == 0) {
		echo "</table>";
	}
	echo "[--------------------------------------------------------------------------------------------]$lbreak";
}

?>