<?php
ini_set('display_errors','On');

require_once( ROOT_PATH . '/engine/global_functions.php' );
require_once( ROOT_PATH . '/functions/parallelcurl.php' );

class PushNotificationPermissions
{
	var $league = array();
	var $teams = array();
}

class liveScoreFunctions extends func
{
	const ERROR_SESSION = 300;
	const ERROR_MISSING = 404;
	
	// TODO: Implement this...
	/*function curl_post_async($url, $params)
	{
		foreach ($params as $key => &$val) {
		  if (is_array($val)) $val = implode(',', $val);
			$post_params[] = $key.'='.urlencode($val);
		}
		$post_string = implode('&', $post_params);

		$parts=parse_url($url);

		$fp = fsockopen($parts['host'],
			isset($parts['port'])?$parts['port']:80,
			$errno, $errstr, 30);

		pete_assert(($fp!=0), "Couldn't open a socket to ".$url." (".$errstr.")");

		$out = "POST ".$parts['path']." HTTP/1.1\r\n";
		$out.= "Host: ".$parts['host']."\r\n";
		$out.= "Content-Type: application/x-www-form-urlencoded\r\n";
		$out.= "Content-Length: ".strlen($post_string)."\r\n";
		$out.= "Connection: Close\r\n\r\n";
		if (isset($post_string)) $out.= $post_string;

		fwrite($fp, $out);
		fclose($fp);
	}*/

	/** * Load and return configuration file of path $path * * @param string $path Configuration file path * * @return array */
	function load( $path )
	{
		if ( file_exists( $path ) )
			return require $path;
		else
			return array();
	}
	/** * Save configuration array $array to path $path * Return true if successful, false otherwise * * @param string $path Configuration file path * @param string $array Configuration array * * @return boolean */
	function save( $path, $array )
	{
		$content = '<?php' . PHP_EOL . 'return ' . var_export( $array, true ) . ';';
		return is_numeric( file_put_contents( $path, $content ) );
	}

	function doAuth()
	{
		$url           = 'https://login.live.com/accesstoken.srf';
		$fields        = array(
			 'grant_type' => "client_credentials",
			'client_id' => urlencode( "ms-app://s-1-15-2-1880801760-643261974-1600809880-3528962424-4240877259-2009888346-2122219974" ),
			'client_secret' => urlencode( "8t1OOJy8BtczWwIeMrUBddfO5URoybn4" ),
			'scope' => "notify.windows.com" 
		);
		$fields_string = ""; //url-ify the data for the POST        
		foreach ( $fields as $key => $value )
		{
			$fields_string .= $key . '=' . $value . '&';
		}
		rtrim( $fields_string, '&' ); //open connection       
		$ch = curl_init(); //set the url, number of POST vars, POST data       
		curl_setopt( $ch, CURLOPT_SSL_VERIFYHOST, 0 );
		curl_setopt( $ch, CURLOPT_SSL_VERIFYPEER, 0 );
		curl_setopt( $ch, CURLOPT_URL, $url );
		curl_setopt( $ch, CURLOPT_POST, count( $fields ) );
		curl_setopt( $ch, CURLOPT_POSTFIELDS, $fields_string );
		curl_setopt( $ch, CURLOPT_HTTPHEADER, array(
			 "Content-Type: application/x-www-form-urlencoded",
			"SOAPAction: \"/soap/action/query\"",
			"Content-length: " . strlen( $fields_string ) 
		) );
		curl_setopt( $ch, CURLOPT_RETURNTRANSFER, TRUE );
		//execute post       
		$result = curl_exec( $ch );
		if ( !curl_errno( $ch ) )
		{
			$info = curl_getinfo( $ch );
			//echo 'Took ' . $info['total_time'] . ' seconds to send a request to ' . $info['url']; 
		}
		else
		{
			echo 'Curl error: ' . curl_error( $ch );
		} //close connection   
		curl_close( $ch ); //echo $result;         
		$jsonData = json_decode($result);      
		//$this->debugArray($jsonData);     
		return $jsonData->access_token;
	}
	
	/*function sendPushRequest( $token, $userid, $url, $message, $image, $message2 )
	{
		global $PC;
		
		$pushXML = "
		<toast>
			<visual>
				<binding template=\"ToastImageAndText03\">
					<image id=\"1\" src=\"ms-appx:///Assets/crests/".$image.".png\"/>
					<text id=\"1\">" . htmlspecialchars($message) . "</text>
					<text id=\"2\">" . htmlspecialchars($message2) . "</text>
				</binding>
			</visual>
			<audio src=\"ms-appdata:///Assets/sfx/goal.wav\" />
		</toast>";
		
		$headers = array(
			"Authorization: Bearer " . $token,
			"Content-Type: text/xml",
			"Content-Length: " . strlen( $pushXML ),
			"X-WNS-Type: wns/toast" 
		);
		
		$options = array( CURLOPT_SSL_VERIFYHOST => 0,
						  CURLOPT_SSL_VERIFYPEER => 0,
						  CURLOPT_HTTPHEADER => $headers,
						  CURLINFO_HEADER_OUT => true,
						  );
		
		$user_data = array("userID"=>$userid);
		$PC->setOptions($options);
		$PC->startRequest($url, array($this, 'on_request_done'), $user_data, $pushXML);

	}*/
	
	function sendPushRequest( $token, $userid, $url, $message, $image, $message2 )
	{
		global $PC;
		
		$pushXML = "
		<toast>
			<visual>
				<binding template=\"ToastImageAndText03\">
					<image id=\"1\" src=\"ms-appx:///Assets/crests/".$image.".png\"/>
					<text id=\"1\">" . htmlspecialchars($message) . "</text>
					<text id=\"2\">" . htmlspecialchars($message2) . "</text>
				</binding>
			</visual>
			<audio src=\"ms-appdata:///Assets/sfx/goal.wav\" />
		</toast>";
		
		$headers = array(
			"Authorization: Bearer " . $token,
			"Content-Type: text/xml",
			"Content-Length: " . strlen( $pushXML ),
			"X-WNS-Type: wns/toast" 
		);
		
		$options = array( CURLOPT_SSL_VERIFYHOST => 0,
						  CURLOPT_SSL_VERIFYPEER => 0,
						  CURLOPT_HTTPHEADER => $headers,
						  CURLINFO_HEADER_OUT => true,
						  );
		
		//$user_data = array("userID"=>$userid);
		//$PC->setOptions($options);
		//$PC->startRequest($url, array($this, 'on_request_done'), $user_data, $pushXML);
		
		$request = new RollingCurlRequest($url, "POST", $pushXML, $headers, $options);
		return $request;
	}
	
	function sendTileRequest( $token, $userid, $url, $message, $image, $message2 )
	{
		global $PC;
		
		$pushXML = "
		<tile>
		  <visual>
			<binding template=\"TileWideSmallImageAndText02\">
			  <image id=\"1\" src=\"ms-appx:///Assets/crests/".$image.".png\"/>
			  <text id=\"1\">".htmlspecialchars($message)."</text>
			  <text id=\"2\">" . htmlspecialchars($message2) . "</text>
			</binding>  
		  </visual>
		</tile>";
		
		$headers = array(
			"Authorization: Bearer " . $token,
			"Content-Type: text/xml",
			"X-WNS-Cache-Policy: cache",
			"Content-Length: " . strlen( $pushXML ),
			"X-WNS-Type: wns/tile" 
		);
		
		$options = array( CURLOPT_SSL_VERIFYHOST => 0,
						  CURLOPT_SSL_VERIFYPEER => 0,
						  CURLOPT_HTTPHEADER => $headers,
						  CURLINFO_HEADER_OUT => true,
						  );
		
		//$user_data = array("userID"=>$userid);
		//$PC->setOptions($options);
		//$PC->startRequest($url, array($this, 'on_request_done'), $user_data, $pushXML);
		
		$request = new RollingCurlRequest($url, "POST", $pushXML, $headers, $options);
		return $request;
	}
	
	function sendBadgeRequest( $token, $url, $count )
	{
		global $PC;
		
		$pushXML = "<badge value=\"{$count}\"/>";
		
		$headers = array(
			"Authorization: Bearer " . $token,
			"Content-Type: text/xml",
			"X-WNS-Cache-Policy: cache",
			"Content-Length: " . strlen( $pushXML ),
			"X-WNS-Type: wns/badge" 
		);
		
		$options = array( CURLOPT_SSL_VERIFYHOST => 0,
						  CURLOPT_SSL_VERIFYPEER => 0,
						  CURLOPT_HTTPHEADER => $headers,
						  CURLINFO_HEADER_OUT => true,
						  );
		
		$request = new RollingCurlRequest($url, "POST", $pushXML, $headers, $options);
		return $request;
	}
	
	function sendPushRequestWP8( $token, $userid, $url, $message, $image, $message2 )
	{
		$pushXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" .
				"<wp:Notification xmlns:wp=\"WPNotification\">" .
				"<wp:Toast>" .
				"<wp:Text1>".htmlspecialchars($message)."</wp:Text1>" .
				"<wp:Text2>".htmlspecialchars($message2)."</wp:Text2>" .
				"</wp:Toast>" .
				"</wp:Notification>";
				
		$sendedheaders = array(
						"X-WindowsPhone-Target: toast",
						"Content-Type: text/xml",
						"Accept: application/*",
						"Content-Length: " . strlen( $pushXML ),
						"X-NotificationClass: 2" 
					);	
					
		$options = array( CURLOPT_HEADER => true,
						  CURLOPT_HTTPHEADER => $sendedheaders,
						  CURLOPT_POST => true,
						  CURLOPT_POSTFIELDS => $pushXML,
						  CURLOPT_URL => $url,
						  CURLOPT_RETURNTRANSFER => 1,
						  );
						  
		$request = new RollingCurlRequest($url, "POST", $pushXML, $sendedheaders, $options);
		
		return $request;
	}
	
	function sendTileRequestWP8( $url, $message, $message2, $message3, $count )
	{
		$pushXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>
					<wp:Notification xmlns:wp=\"WPNotification\" Version=\"2.0\">
					  <wp:Tile Id=\"ScoreAlertsMobileToken\" Template=\"IconicTile\">
						<wp:SmallIconImage>Assets/Tiles/IconicTileSmall.png</wp:SmallIconImage>
						<wp:IconImage>Assets/Tiles/IconicTileMediumLarge.png</wp:IconImage>
						<wp:WideContent1>".htmlspecialchars($message)."</wp:WideContent1>
						<wp:WideContent2>".htmlspecialchars($message2)."</wp:WideContent2>
						<wp:WideContent3>".htmlspecialchars($message3)."</wp:WideContent3>
						<wp:Count>".$count."</wp:Count>
						<wp:Title>Score Alerts</wp:Title>
						<wp:BackgroundColor Action=\"Clear\">#00ffffff</wp:BackgroundColor>
					  </wp:Tile>
					</wp:Notification>";
					
				
		$sendedheaders = array(
						"X-WindowsPhone-Target: token",
						"Content-Type: text/xml",
						"Accept: application/*",
						"Content-Length: " . strlen( $pushXML ),
						"X-NotificationClass: 1" 
					);    
					
		$options = array( CURLOPT_HEADER => true,
						  CURLOPT_HTTPHEADER => $sendedheaders,
						  CURLOPT_POST => true,
						  CURLOPT_POSTFIELDS => $pushXML,
						  CURLOPT_URL => $url,
						  CURLOPT_RETURNTRANSFER => 1,
						  );
						  
		$request = new RollingCurlRequest($url, "POST", $pushXML, $sendedheaders, $options);
		
		return $request;
	}
	
	function getCertificate($certID)
	{
		$url  = 'https://lic.apps.microsoft.com/licensing/certificateserver/?cid='.$certID;
		$path = '/home1/rwscrip1/public_html/scorealerts/certs/'.$certID;
	 
		if ( !file_exists($path) )
		{
			$fp = fopen($path, 'w');
		 
			$ch = curl_init($url);
			curl_setopt($ch, CURLOPT_FILE, $fp);
		 
			$data = curl_exec($ch);
		 
			curl_close($ch);
			fclose($fp);
		}
		$cert = file_get_contents($path);
		//var_dump(openssl_x509_parse($cert));
		
		//return openssl_x509_read($cert);
		return $cert;
	}
	
	// This function gets called back for each request that completes
	function on_WP8_request_done($content, $url, $ch, $data) 
	{
		global $DB;
		
		//echo "Result: ".$content;
		
		$httpcode = curl_getinfo($ch, CURLINFO_HTTP_CODE);    
		if ($httpcode !== 200) 
		{
			if ( $httpcode == 410 )
			{
				$DB->query("DELETE FROM `pushtable` WHERE `userID`='".$data['userID']."'");
				
				$myFile = "pushLog.txt";
				$fh = fopen($myFile, 'a');
				$stringData = "Error 410 so calling DELETE FROM `pushtable` WHERE `userID`='".$data['userID']."'";
				fwrite($fh, $stringData);
				fclose($fh);   
			}
			else
			{
				print "Fetch error $httpcode for '$url'\n";
				$myFile = "pushLog.txt";
				$fh = fopen($myFile, 'a');
				$stringData = "Fetch error $httpcode for '$url'\n";
				fwrite($fh, $stringData);
				fclose($fh);   
			}
			return;
		}
	 
		/*$responseobject = json_decode($content, true);
		if (empty($responseobject['responseData']['results'])) 
		{
			print "No results found for '$search'\n";
			return;
		}
	 
		print "********\n";
		print "$search:\n";
		print "********\n";
	 
		$allresponseresults = $responseobject['responseData']['results'];
		foreach ($allresponseresults as $responseresult) {
			$title = $responseresult['title'];
			print "$title\n";
		}*/
	}
	
	// This function gets called back for each request that completes
	function on_request_done($content, $url, $ch, $data) 
	{
		global $DB;
		$httpcode = curl_getinfo($ch, CURLINFO_HTTP_CODE);    
		
		if ($httpcode !== 200) 
		{
			if ( $httpcode == 410 )
			{
				$DB->query("DELETE FROM `pushtable` WHERE `userID`='".$data['userID']."'");
				
				$myFile = "pushLog.txt";
				$fh = fopen($myFile, 'a');
				$stringData = "Error 410 so calling DELETE FROM `pushtable` WHERE `userID`='".$data['userID']."'";
				fwrite($fh, $stringData);
				fclose($fh);   
			}
			else if ( $httpcode == 400 )
			{
				echo "Bad headers\n";
				$headers = curl_getinfo($ch, CURLINFO_HEADER_OUT);
				echo $headers;
			}
			else if ( $httpcode == 405 )
			{
				echo "Invalid method (GET, DELETE, CREATE); only POST is allowed\n";
				$headers = curl_getinfo($ch, CURLINFO_HEADER_OUT);
				echo $headers;
			}
			else
			{
				print "Fetch error $httpcode for '$url'\n";
				$myFile = "pushLog.txt";
				$fh = fopen($myFile, 'a');
				$stringData = "Fetch error $httpcode for '$url'\n";
				fwrite($fh, $stringData);
				fclose($fh);   
			}
			return;
		}
	 
		/*$responseobject = json_decode($content, true);
		if (empty($responseobject['responseData']['results'])) 
		{
			print "No results found for '$search'\n";
			return;
		}
	 
		print "********\n";
		print "$search:\n";
		print "********\n";
	 
		$allresponseresults = $responseobject['responseData']['results'];
		foreach ($allresponseresults as $responseresult) {
			$title = $responseresult['title'];
			print "$title\n";
		}*/
	}
	
	function microtime_float()
	{
		list($usec, $sec) = explode(" ", microtime());
		return ((float)$usec + (float)$sec);
	}
	
	function get_data( $url )
	{
		$ch      = curl_init();
		$timeout = 5;
		curl_setopt( $ch, CURLOPT_URL, $url );
		curl_setopt( $ch, CURLOPT_RETURNTRANSFER, 1 );
		curl_setopt( $ch, CURLOPT_CONNECTTIMEOUT, $timeout );
		$data = curl_exec( $ch );
		curl_close( $ch );
		return $data;
	}
	
	function parseStatusDetails( $fixture, $link )
	{
		$childLink = $this->findNode( $link, "a", null );
		if ( $childLink != null )
			$fixture->MatchDetails = $childLink->getAttribute( "href" );
	}
	function parseKickoffDetails( $fixture, $link )
	{
		//echo "Home score: ".$fixture->HomeScore;
		if ( !is_numeric($fixture->HomeScore) )
		{
			$fixture->MatchInfo = "Postponed";
		}
		else
		{
			$childLink          = $this->findNode( $link, "#text", null );
			$fixture->MatchInfo = $this->makeSafe( trim( $childLink->nodeValue ) );   
		}
	}
	function findNode( $link, $nodeName, $class=null )
	{
		if ( $link->nodeName == $nodeName && ( $class == null || ( $link->getAttribute( "class" ) != null && $link->getAttribute( "class" ) == $class ) ) )
			return $link;
		if ( $link->childNodes != null )
		{
			foreach ( $link->childNodes as $child )
			{
				$found = $this->findNodeChild( $child, $nodeName, $class );
				if ( $found != null )
					return $found;
			}
		}
		return null;
	}
	
	function findNodeChild( $link, $nodeName, $class=null )
	{
		if ( $link->nodeName == $nodeName && ( $class == null || ( $link->getAttribute( "class" ) != null && $link->getAttribute( "class" ) == $class ) ) )
			return $link;
		if ( $link->childNodes != null )
		{
			foreach ( $link->childNodes as $child )
			{
				$found = $this->findNode( $child, $nodeName, $class );
				if ( $found != null )
					return $found;
			}
		}
		while ( $link->nextSibling != null )
		{
			$found = $this->findNode( $link->nextSibling, $nodeName, $class );
			if ( $found != null )
				return $found;
			$link = $link->nextSibling;
		}
		return null;
	}
	
	function parseMatchDetails( $fixture, $dom, $link )
	{
		//echo $dom->saveHTML($link);
		
		$homeTeamLink      = $this->findNode( $link, "span", "team-home teams" );
		$fixture->HomeTeam = $this->getTeamName( $homeTeamLink );
		$awayTeamLink      = $this->findNode( $link, "span", "team-away teams" );
		$fixture->AwayTeam = $this->getTeamName( $awayTeamLink );
		$scoreLink         = $this->findNode( $link, "span", "score" );
		if ( $scoreLink != null )
		{
			$scoreLink         = $this->findNode( $scoreLink, "abbr" );
			$score              = trim( $scoreLink->nodeValue );
			$parts              = explode( "-", $score );
			$newHomeScore       = $parts[0];
			$newAwayScore       = $parts[1];
			$fixture->HomeScore = $newHomeScore;
			$fixture->AwayScore = $newAwayScore;
		}
		//echo $fixture->HomeTeam . " v " . $fixture->AwayTeam;
	}
	function getTeamName( $link )
	{
		$childLink = $this->findNode( $link, "a", null );		
		if ( $childLink == null )
			return addslashes(trim($link->nodeValue));
		else
			return addslashes(trim($childLink->nodeValue));
	}
	
	function immediateEcho($output)
	{
		//echo $output."<br>";
		//flush();
		//ob_flush();
	}
	
	function resolveSession($sessionID)
	{
		global $DB;
		$DB->query("SELECT * FROM sessions WHERE `sessionID`='$sessionID'");
		$myrow = $DB->fetch_row();
		if ( $myrow != null )
			return $myrow['userID'];
		else
			return -1;
	}
}
function dbg_curl_data( $curl, $data = null )
{
	static $buffer = '';
	if ( is_null( $curl ) )
	{
		$r      = $buffer;
		$buffer = '';
		return $r;
	}
	else
	{
		$buffer .= $data;
		return strlen( $data );
	}
}

?>
