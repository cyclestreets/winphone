<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/communicate.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

class communicate
{
	var $use_sockets = 0;
	var $errors      = array();
	var $key_prefix = '__rwd__';
	var $auth_req       = 0;
	var $auth_user;
	var $auth_pass;
	
	function communicate()
	{

	}

	function receive_data( $return_fields=array() )
	{
		$return_array = array();
		
		foreach( $_REQUEST as $k => $v )
		{
			if ( strstr( $k, $this->key_prefix ) )
			{
				$k = str_replace( $this->key_prefix, '', $k );
				
				$return_array[ $k ] = $v;
			}
		}
		
		return $this->filter_fields( $return_array );
	}
	
	function send_data( $file_location='', $post_array=array() )
	{
		if ( ! is_array( $post_array ) OR ! count( $post_array ) )
		{
			return FALSE;
		}
		
		if ( ! $file_location )
		{
			return FALSE;
		}
		
		return $this->post_data( $file_location, $post_array );
	}
	
	function filter_fields( $in_fields=array(), $out_fields=array() )
	{
		$return_array = array();
		
		if ( ! is_array( $in_fields ) or ! count( $in_fields ) )
		{
			return FALSE;
		}
		
		if ( ! is_array( $out_fields ) or ! count( $out_fields ) )
		{
			return $in_fields;
		}
		
		foreach( $out_fields as $k => $type )
		{
			if ( $in_fields[ $k ] )
			{
				switch ( $type )
				{
					default:
					case 'string':
					case 'text':
						$return_array[ $k ] = trim( $in_fields[ $k ] );
						break;
					case 'int':
					case 'integar':
						$return_array[ $k ] = intval( $in_fields[ $k ] );
						break;
					case 'float':
					case 'floatval':
						$return_array[ $k ] = floatval( $in_fields[ $k ] );
						break;
				}
			}
		}
		
		return $return_array;
	}
	
	function post_data( $file_location, $post_array )
	{
		$data            = null;
		$fsocket_timeout = 10;
		$post_back       = array();
		
		foreach ( $post_array as $key => $val )
		{
			$post_back[] = $this->key_prefix . $key . '=' . urlencode($val);
		}
		
		$post_back_str = implode('&', $post_back);
		
		$url_parts = parse_url($file_location);
		
		if ( ! $url_parts['host'] )
		{
			$this->errors[] = "No host found in the URL '$file_location'!";
			return FALSE;
		}
		
		$host = $url_parts['host'];
      	$port = ( isset($url_parts['port']) ) ? $url_parts['port'] : 80;
      	
      	if ( ! empty( $url_parts["path"] ) )
		{
			$path = $url_parts["path"];
		}
		else
		{
			$path = "/";
		}
 
		if ( ! empty( $url_parts["query"] ) )
		{
			$path .= "?" . $url_parts["query"];
		}

        // Try curl first as it supports more features
		if ( function_exists("curl_init") )
		{
			if ( $sock = curl_init() )
			{
				curl_setopt( $sock, CURLOPT_URL            , $file_location );
				curl_setopt( $sock, CURLOPT_TIMEOUT        , 15 );
				curl_setopt( $sock, CURLOPT_POST           , TRUE );
				curl_setopt( $sock, CURLOPT_POSTFIELDS     , $post_back_str );
				curl_setopt( $sock, CURLOPT_POSTFIELDSIZE  , 0);
				curl_setopt( $sock, CURLOPT_RETURNTRANSFER , TRUE ); 
		
				$result = curl_exec($sock);
				
				curl_close($sock);
				
				return $result ? $result : FALSE;
			}
		}
      	else
		{
      
  	    	if ( ! $fp = @fsockopen( $host, $port, $errno, $errstr, $fsocket_timeout ) )
	      	{
				$this->errors[] = "CONNECTION REFUSED FROM $host. Error $errno: $errstr";
				return FALSE;
         
			}
			else
			{
				$final_carriage = "";
				
				if ( ! $this->auth_req )
				{
					$final_carriage = "\r\n";
				}
				
				$header  = "POST $path HTTP/1.0\r\n";
				$header .= "Host: $host\r\n";
				$header .= "Content-Type: application/x-www-form-urlencoded\r\n";
				$header .= "Content-Length: " . strlen($post_back_str) . "\r\n{$final_carriage}";
				
				if ( ! fputs( $fp, $header . $post_back_str ) )
				{
					$this->errors[] = "Unable to send request to $host!";
					return FALSE;
				}
			
				if ( $this->auth_req )
				{
					if( $this->auth_user and $this->auth_pass )
					{
						$header = "Authorization: Basic ".base64_encode("{$this->auth_user}:{$this->auth_pass}")."\r\n\r\n";
						
						if ( ! fputs( $fp, $header ) )
						{
							$this->errors[] = "Authorization Failed!";
							return FALSE;
						}
					}
				}				
	         }

	         @stream_set_timeout($fp, $fsocket_timeout);
         	 
	         $status = @socket_get_status($fp);
         
	         while( ! feof($fp) and ! $status['timed_out'] )         
	         {
	            $data .= fgets ($fp,8192);
	            $status = socket_get_status($fp);
	         }
         
	         fclose ($fp);
         
	         // Strip headers
	         $tmp = split("\r\n\r\n", $data, 2);
	         $data = $tmp[1];

	 		return $data;
		}
	}

    function readRemoteFile($url)
    {
        if (ini_get('allow_url_fopen') == '1')
        {
            $content = file_get_contents($url);
            if ($content !== false)
            {
               return $content;
            }
            else
            {
               return 0;
            }
        }
        else
        {
           // make sure curl is installed
           if (function_exists('curl_init'))
           {
               // initialize a new curl resource
               $ch = curl_init();

               // set the url to fetch
               curl_setopt($ch, CURLOPT_URL, $url);

               // don't give me the headers just the content
               curl_setopt($ch, CURLOPT_HEADER, 0);

               // return the value instead of printing the response to browser
               curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);

               // use a user agent to mimic a browser
               curl_setopt($ch, CURLOPT_USERAGENT, 'Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.7.5) Gecko/20041107 Firefox/1.0');

               $content = curl_exec($ch);

               // remember to always close the session and free all resources
               curl_close($ch);

               return $content;
            }
            else
            {
               return getRemoteFile($url);
            }
        }
        return 0;
    }

    function getRemoteFile($url)
    {
       // get the host name and url path
       $parsedUrl = parse_url($url);
       $host = $parsedUrl['host'];
       if (isset($parsedUrl['path'])) {
          $path = $parsedUrl['path'];
       } else {
          // the url is pointing to the host like http://www.mysite.com
          $path = '/';
       }

       if (isset($parsedUrl['query'])) {
          $path .= '?' . $parsedUrl['query'];
       }

       if (isset($parsedUrl['port'])) {
          $port = $parsedUrl['port'];
       } else {
          // most sites use port 80
          $port = '80';
       }

       $timeout = 10;
       $response = '';

       // connect to the remote server
       $fp = @fsockopen($host, '80', $errno, $errstr, $timeout );

       if( !$fp ) {
          echo "Cannot retrieve $url";
       } else {
          // send the necessary headers to get the file
          fputs($fp, "GET $path HTTP/1.0\r\n" .
                     "Host: $host\r\n" .
                     "User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.3) Gecko/20060426 Firefox/1.5.0.3\r\n" .
                     "Accept: */*\r\n" .
                     "Accept-Language: en-us,en;q=0.5\r\n" .
                     "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7\r\n" .
                     "Keep-Alive: 300\r\n" .
                     "Connection: keep-alive\r\n" .
                     "Referer: http://$host\r\n\r\n");

          // retrieve the response from the remote server
          while ( $line = fread( $fp, 4096 ) ) {
             $response .= $line;
          }

          fclose( $fp );

          // strip the headers
          $pos      = strpos($response, "\r\n\r\n");
          $response = substr($response, $pos + 4);
       }

       // return the file content
       return $response;
    }
}

?>
